using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpendLensDatabase.Models;

[Serializable]
public class Membership
{
    public required Guid UserId { get; set; }
    public required Guid OrganizationId { get; set; }
    
    public required MembershipRole Role { get; set; }
    public bool IsDeleted { get; set; }
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}

file sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships", SpendLensDbContext.PublicSchema);
        
        builder.HasKey(m => new { m.UserId, m.OrganizationId });

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<byte>();
        
        builder.HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(m => m.Organization)
            .WithMany(o => o.Memberships)
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public enum MembershipRole : byte
{
    Viewer,
    Accountant,
    Owner
}