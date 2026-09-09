using FluentValidation;

namespace SpendLensApi.Auth.Registration;

public sealed class OrganizationModelValidator : AbstractValidator<OrganizationModel>
{
    public OrganizationModelValidator()
    {
        RuleFor(model => model.Name)
            .NotEmpty();
    }
}