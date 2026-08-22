namespace SpendLensDatabase.Models.Auth;

public abstract record RegisterResult
{
    private RegisterResult() {}

    public sealed record Success(Guid UserId) : RegisterResult;
    
    public sealed record EmailTaken: RegisterResult;
}