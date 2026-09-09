using FluentValidation;

namespace SpendLensApi.Contracts.Users;

public sealed class UserModelValidator : AbstractValidator<UserModel>
{
    public UserModelValidator()
    {
        RuleFor(model => model.Email)
            .NotEmpty()
            .EmailAddress();
        
        RuleFor(model => model.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}