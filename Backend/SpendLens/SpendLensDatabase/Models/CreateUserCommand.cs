namespace SpendLensDatabase.Models;

public sealed record CreateUserCommand(string Email, string Password);

public sealed record CreateOrganizationCommand(string Name);

public sealed record CreateAuthCommand(CreateUserCommand User, CreateOrganizationCommand Organization);

public abstract record RegisterResult
{
    private RegisterResult() {}

    public sealed record Success(Guid UserId) : RegisterResult;
    
    public sealed record EmailTaken: RegisterResult;
}