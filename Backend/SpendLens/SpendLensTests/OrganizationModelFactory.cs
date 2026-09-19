using SpendLensApi.Contracts.Organizations;

namespace SpendLensTests;

public static class OrganizationModelFactory
{
    public static OrganizationModel CreateOrganizationModel(string? name = null) =>
        new(name ?? Guid.NewGuid().ToString("N"));
}