using SpendLensDatabase.Models.Auth.Users;

namespace SpendLensDatabase.Models.Auth;

public abstract record RegisterResult
{
    private RegisterResult() {}

    public sealed record Success(UserDto User, string RawToken) : RegisterResult;
    
    public sealed record EmailTaken: RegisterResult;
}