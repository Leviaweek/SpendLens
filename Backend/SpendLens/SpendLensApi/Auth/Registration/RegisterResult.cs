using SpendLensApi.Contracts.Users;

namespace SpendLensApi.Auth.Registration;

public abstract record RegisterResult
{
    public sealed record Success(UserDto User, string RefreshToken) : RegisterResult;
    
    public sealed record EmailTaken: RegisterResult;
}