using SpendLensApi.Contracts.Users;

namespace SpendLensApi.Auth.RefreshTokens;

public abstract record RefreshResult
{
    public sealed record Success(string RawToken, UserDto User) : RefreshResult;
    public sealed record ReuseOrNotFound: RefreshResult;
    
}