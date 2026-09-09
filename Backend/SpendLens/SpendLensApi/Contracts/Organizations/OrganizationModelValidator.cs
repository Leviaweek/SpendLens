using FluentValidation;

namespace SpendLensApi.Contracts.Organizations;

public sealed class OrganizationModelValidator : AbstractValidator<OrganizationModel>
{
    public OrganizationModelValidator()
    {
        RuleFor(model => model.Name)
            .NotEmpty();
    }
}