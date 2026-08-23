using SpendLensDatabase.Models.Auth.User;

namespace SpendLensDatabase.Models.Auth;

public abstract record RegisterResult
{
    private RegisterResult() {}

    public sealed record Success(UserDto User) : RegisterResult;
    
    public sealed record EmailTaken: RegisterResult;
}