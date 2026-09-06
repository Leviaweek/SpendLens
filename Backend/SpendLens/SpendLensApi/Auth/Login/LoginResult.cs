using SpendLensDatabase.Models.AuthData.Users;

namespace SpendLensApi.Auth.Login;

public record LoginResult
{
    public sealed record Success(UserDto User, string RefreshToken) : LoginResult;

    public record Unauthorized : LoginResult;
}