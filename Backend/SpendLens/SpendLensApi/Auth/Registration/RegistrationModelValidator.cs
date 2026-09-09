using FluentValidation;

namespace SpendLensApi.Auth.Registration;

public sealed class RegistrationModelValidator : AbstractValidator<RegistrationModel>
{
    public RegistrationModelValidator(
        IValidator<UserModel> userCreationModelValidator,
        IValidator<OrganizationModel> organizationCreationModelValidator)
    {
        RuleFor(model => model.User)
            .SetValidator(userCreationModelValidator);

        RuleFor(model => model.Organization)
            .SetValidator(organizationCreationModelValidator);
    }
}