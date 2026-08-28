using SpendLensDatabase.Models.Auth.Users;

namespace SpendLensDatabase.Models.Auth;

public record LoginResult
{
    public sealed record Success(UserDto User, string RefreshToken) : LoginResult;

    public record Failure : LoginResult;
}