using SpendLensDatabase;
using SpendLensDatabase.Models.Entities;

namespace SpendLensTests;

public static class TestDataSeeder
{
    public static async Task<(User User, string Password)> SeedUserAsync(
        SpendLensDbContext context,
        string? email = null,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        var (user, actualPassword) = password is null
            ? UserFactory.CreateUser(email)
            : UserFactory.CreateUser(email, password);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return (user, actualPassword);
    }
}