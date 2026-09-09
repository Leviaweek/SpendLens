using Microsoft.EntityFrameworkCore;
using Npgsql;
using SpendLensApi.Auth.RefreshTokens;
using SpendLensApi.Contracts.Users;
using SpendLensDatabase;
using SpendLensDatabase.Models;
using SpendLensDatabase.Models.Entities;

namespace SpendLensApi.Auth.Registration;

public sealed class RegistrationService(SpendLensDbContext context, RefreshTokenService refreshTokenService)
{
    public async Task<RegisterResult> CreateAuthModelsAsync(RegistrationModel data,
        TimeSpan refreshTokenLifetime,
        CancellationToken cancellationToken)
    {
        var email = data.User.Email.ToLowerInvariant();
        
        var user = await GetUserOrDefaultAsync(email, cancellationToken);

        if (user is not null)
            return new RegisterResult.EmailTaken();

        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.User.Password),
            CreatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(newUser);

        var newOrganization = new Organization
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = DateTime.UtcNow,
            Name = data.Organization.Name,
        };
        
        context.Organizations.Add(newOrganization);

        var membership = new Membership
        {
            OrganizationId = newOrganization.Id,
            UserId = newUser.Id,
            Role = MembershipRole.Owner
        };

        context.Memberships.Add(membership);
        
        var rawToken = refreshTokenService.AddRefreshToken(refreshTokenLifetime, newUser);

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

    private async Task<User?> GetUserOrDefaultAsync(string email, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync(cancellationToken);
        return user;
    }

}