using SpendLensDatabase.Models.AuthData.Users;

namespace SpendLensApi.Auth.RefreshTokens;

public abstract record RotateResult
{
    public sealed record Success(string RawToken, UserDto User) : RotateResult;
    public sealed record ReuseOrNotFound: RotateResult;
    
}