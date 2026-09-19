using SpendLensApi.Contracts.Users;

namespace SpendLensTests;

public static class UserModelFactory
{
    private const string DefaultPassword = "P@ssw0rd123!";

    public static UserModel CreateUserModel(string? email = null, string password = DefaultPassword) =>
        new(
            Email: email ?? $"{Guid.NewGuid():N}@gmail.com",
            Password: password);
}