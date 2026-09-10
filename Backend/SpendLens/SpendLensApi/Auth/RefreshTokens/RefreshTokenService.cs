using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SpendLensApi.Contracts.Users;
using SpendLensDatabase;
using SpendLensDatabase.Models.Entities;

namespace SpendLensApi.Auth.RefreshTokens;

public sealed class RefreshTokenService(SpendLensDbContext context, ILogger<RefreshTokenService> logger)
{
    public const string CookieName = "refreshToken";
    
    public string AddRefreshToken(TimeSpan refreshTokenLifetime, User user)
    {
        var (rawToken, tokenId, verifierHash) = RefreshTokenGenerator.Generate();

        var refreshToken = new RefreshToken
        {
            Id = tokenId,
            UserId = user.Id,
            TokenHash = verifierHash,
            ExpiresAt = DateTime.UtcNow.Add(refreshTokenLifetime),
            RevokedAt = null
        };
        
        context.RefreshTokens.Add(refreshToken);
        
        return rawToken;
    }

    public async Task<bool> ValidateAsync(Guid id, Guid verifier, CancellationToken cancellationToken)
    {
        var token = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        
        if (token is null) 
            return false;

        if (token.RevokedAt is not null) 
            return false;

        if (token.ExpiresAt < DateTime.UtcNow)
            return false;
        
        var hash = SHA256.HashData(verifier.ToByteArray());
        
        return CryptographicOperations.FixedTimeEquals(hash, token.TokenHash);
    }

    public async Task<RefreshResult> RotateAsync(Guid id, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var revoked = await context.RefreshTokens
            .Where(t => t.Id == id)
            .Where(t => t.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), 
                cancellationToken);

        if (revoked == 0)
        {
            logger.LogWarning("Failed to rotate refresh token for user {UserId}", id);
            return new RefreshResult.ReuseOrNotFound();
        }

        var userDto = await context.RefreshTokens
            .Where(t => t.Id == id)
            .Select(t => t.User)
            .Select(UserDto.FromUser)
            .FirstAsync(cancellationToken);

        var (rawToken, tokenId, verifierHash) = RefreshTokenGenerator.Generate();

        var newToken = new RefreshToken
        {
            Id = tokenId,
            UserId = userDto.Id,
            TokenHash = verifierHash,
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            RevokedAt = null
        };

        context.RefreshTokens.Add(newToken);

        await context.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation(
            "Refresh token rotated for user {UserId}",
            userDto.Id);

        return new RefreshResult.Success(rawToken, userDto);
    }
}