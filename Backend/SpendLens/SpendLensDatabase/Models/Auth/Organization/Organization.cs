using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpendLensDatabase.Models.Auth.Organization;

public class Organization
{
    public Guid Id { get; init; }
    
    public required string Name { get; init; }
    
    public required DateTime CreatedAt { get; init; }
    public ICollection<Membership> Memberships { get; init; } = [];
}

file sealed class OrganizationConfigure: IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations", SpendLensDbContext.PublicSchema);
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasConversion(dw => DateTime.SpecifyKind(dw, DateTimeKind.Utc), dr => dr);
    }
}