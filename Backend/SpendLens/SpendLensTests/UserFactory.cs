using SpendLensDatabase.Models.Entities;

namespace SpendLensTests;

public static class UserFactory
{
    private const string DefaultPassword = "P@ssw0rd123!";
    private const int TestWorkFactor = 4;

    public static (User User, string Password) CreateUser(
        string? email = null,
        string password = DefaultPassword,
        DateTime? createdAt = null)
    {
        var id = Guid.CreateVersion7();
        
        var user = new User
        {
            Id = id,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Email = email ?? $"user{id:N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, TestWorkFactor)
        };

        return (user, password);
    }
}