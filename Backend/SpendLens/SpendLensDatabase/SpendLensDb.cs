using Microsoft.EntityFrameworkCore;
using Npgsql;
using SpendLensDatabase.Models;
using SpendLensDatabase.Models.Auth;
using SpendLensDatabase.Models.Auth.Organization;
using SpendLensDatabase.Models.Auth.Users;


namespace SpendLensDatabase;

public sealed class SpendLensDb(IDbContextFactory<SpendLensDbContext> factory)
{
    private const string DummyPassword = "dummy1";
    private const string DummyHash = "$2a$11$Z4gsv8S3WNaIP/uefFyxAOu4ghKbfz8K9m5IwTuS74NejWQ5n7KRe";

    public async Task<RegisterResult> CreateAuthModelsAsync(RegistrationModel data,
        TimeSpan refreshTokenLifetime,
        CancellationToken cancellationToken)
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

        var rawToken = AddRefreshToken(refreshTokenLifetime, newUser, context);

        context.Users.Add(newUser);
        context.Organizations.Add(newOrganization);
        context.Memberships.Add(membership);

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

    private static string AddRefreshToken(TimeSpan refreshTokenLifetime, User user, SpendLensDbContext context)
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

    public async Task<LoginResult> LoginAsync(UserCreationModel creationModel,
        TimeSpan refreshTokenLifetime,
        CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == creationModel.Email,
            cancellationToken: cancellationToken);

        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(DummyPassword, DummyHash);
            return new LoginResult.Unauthorized();
        }
        
        var verifyResult = BCrypt.Net.BCrypt.Verify(creationModel.Password, user.PasswordHash);

        if (!verifyResult)
            return new LoginResult.Unauthorized();

        var rawToken = AddRefreshToken(refreshTokenLifetime, user, context);
        
        await context.SaveChangesAsync(cancellationToken);
        
        var userDto = new UserDto(user.Id, user.Email, user.CreatedAt);

        return new LoginResult.Success(userDto, rawToken);
    }
}