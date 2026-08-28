using SpendLensDatabase.Models.Auth.Users;

namespace SpendLensDatabase.Models.Auth;

public abstract record RegisterResult
{
    public sealed record Success(UserDto User, string RefreshToken) : RegisterResult;
    
    public sealed record EmailTaken: RegisterResult;
}