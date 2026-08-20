using Microsoft.EntityFrameworkCore;
using Npgsql;
using SpendLensDatabase.Models;


namespace SpendLensDatabase;

public sealed class SpendLensDb(IDbContextFactory<SpendLensDbContext> factory)
{
    public async Task<RegisterResult> CreateAuthModelsAsync(CreateAuthCommand command, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
    
        var email = command.User.Email.ToLowerInvariant();
        
        var user = await context.Users
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is not null)
            return new RegisterResult.EmailTaken();

        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.User.Password),
            CreatedAt = DateTime.UtcNow
        };

        var newOrganization = new Organization
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = DateTime.UtcNow,
            Name = command.Organization.Name,
        };

        var membership = new Membership
        {
            OrganizationId = newOrganization.Id,
            UserId = newUser.Id,
            Role = MembershipRole.Owner
        };
    
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
        return new RegisterResult.Success(newUser.Id);
    }
}