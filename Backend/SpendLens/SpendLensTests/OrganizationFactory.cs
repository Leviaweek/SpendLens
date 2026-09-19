using SpendLensDatabase.Models.Entities;

namespace SpendLensTests;

public static class OrganizationFactory
{
    public static Organization CreateOrganization(string? name = null)
    {
        var id = Guid.CreateVersion7();
        
        return new Organization
        {
            Id = id,
            Name = name ?? id.ToString("N"),
            CreatedAt = DateTime.UtcNow,
        };
    }
}