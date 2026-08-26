using SpendLensDatabase.Models.Auth.Organization;
using SpendLensDatabase.Models.Auth.Users;

namespace SpendLensDatabase.Models.Auth;

public sealed record RegistrationModel(UserCreationModel User, OrganizationCreationModel Organization);