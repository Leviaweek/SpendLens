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

    /// <summary>
    /// Validates a refresh token: exists, not revoked, not expired, hash matches.
    /// <para>
    /// If an already-revoked token is presented again within 1 minute of its revocation,
    /// it's treated as a duplicate network retry — returns <see cref="RefreshTokenValidationResult.Invalid"/>
    /// without side effects. If more than 1 minute has passed, it's treated as a suspected
    /// token theft — all the user's currently active refresh tokens are revoked.
    /// </para>
    /// </summary>
    /// <param name="id">The ID of the stored refresh token record.</param>
    /// <param name="verifier">The verifier part of the token, compared against the stored hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <see cref="RefreshTokenValidationResult.Success"/> if valid;
    /// <see cref="RefreshTokenValidationResult.NotFound"/> if no token with this Id exists;
    /// <see cref="RefreshTokenValidationResult.Invalid"/> if expired, hash mismatch, or revoked
    /// less than a minute ago (treated as a benign retry);
    /// <see cref="RefreshTokenValidationResult.ReuseDetected"/> if revoked more than a minute ago
    /// (treated as suspected theft — triggers revoke-all).
    /// </returns>
    public async Task<RefreshTokenValidationResult> ValidateAsync(
        Guid id,
        Guid verifier,
        CancellationToken cancellationToken)
    {
        var token = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (token is null)
        {
            logger.LogDebug(
                "Refresh token not found {RefreshTokenId}",
                id);

            return new RefreshTokenValidationResult.NotFound();
        }

        var now = DateTime.UtcNow;

        if (token.RevokedAt is not null)
        {
            if (now - token.RevokedAt > TimeSpan.FromMinutes(1))
            {
                await context.RefreshTokens
                    .Where(rt => rt.UserId == token.UserId)
                    .Where(rt => rt.RevokedAt == null)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(rt => rt.RevokedAt, now),
                        cancellationToken);

                logger.LogWarning(
                    "Refresh token reuse detected; revoked all active refresh tokens for user {UserId}",
                    token.UserId);

                return new RefreshTokenValidationResult.ReuseDetected();
            }

            logger.LogDebug(
                "Refresh token {RefreshTokenId} is revoked",
                id);

            return new RefreshTokenValidationResult.Invalid();
        }

        if (token.ExpiresAt < now)
            return new RefreshTokenValidationResult.Invalid();

        var hash = SHA256.HashData(verifier.ToByteArray());

        return CryptographicOperations.FixedTimeEquals(hash, token.TokenHash)
            ? new RefreshTokenValidationResult.Success()
            : new RefreshTokenValidationResult.Invalid();
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
            logger.LogWarning(
                "Failed to rotate refresh token {RefreshTokenId}",
                id);
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