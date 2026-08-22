using SpendLensDatabase.Models.Auth.Organization;
using SpendLensDatabase.Models.Auth.User;

namespace SpendLensDatabase.Models.Auth;

public sealed record RegistrationModel(UserCreationModel User, OrganizationCreationModel Organization);