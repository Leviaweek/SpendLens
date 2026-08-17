using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpendLensDatabase.Models;

[Serializable]
public class Organization
{
    public Guid Id { get; set; }
    
    public required string Name { get; set; }
    
    public required DateTime CreatedAt { get; set; }
    public ICollection<Membership> Memberships { get; set; } = [];
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