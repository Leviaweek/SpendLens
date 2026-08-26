using Microsoft.EntityFrameworkCore;
using Npgsql;
using SpendLensDatabase.Models;
using SpendLensDatabase.Models.Auth;
using SpendLensDatabase.Models.Auth.Organization;
using SpendLensDatabase.Models.Auth.Users;


namespace SpendLensDatabase;

public sealed class SpendLensDb(IDbContextFactory<SpendLensDbContext> factory)
{
    public async Task<RegisterResult> CreateAuthModelsAsync(RegistrationModel data, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
    
        var email = data.User.Email.ToLowerInvariant();
        
        var user = await context.Users
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is not null)
            return new RegisterResult.EmailTaken();

        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.User.Password),
            CreatedAt = DateTime.UtcNow
        };

        var newOrganization = new Organization
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = DateTime.UtcNow,
            Name = data.Organization.Name,
        };

        var membership = new Membership
        {
            OrganizationId = newOrganization.Id,
            UserId = newUser.Id,
            Role = MembershipRole.Owner
        };

        var (rawToken, tokenId, verifierHash) = RefreshTokenGenerator.Generate();

        var refreshToken = new RefreshToken
        {
            Id = tokenId,
            UserId = newUser.Id,
            TokenHash = verifierHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            RevokedAt = null
        };
        
        context.Users.Add(newUser);
        context.Organizations.Add(newOrganization);
        context.Memberships.Add(membership);
        context.RefreshTokens.Add(refreshToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return new RegisterResult.EmailTaken();
        }
        var userDto = new UserDto(newUser.Id, newUser.Email, newUser.CreatedAt);
        return new RegisterResult.Success(userDto, rawToken);
    }
}