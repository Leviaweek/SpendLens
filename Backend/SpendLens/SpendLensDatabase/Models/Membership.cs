using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpendLensDatabase.Models.Auth.Organization;
using SpendLensDatabase.Models.Auth.User;

namespace SpendLensDatabase.Models;

public class Membership
{
    public required Guid UserId { get; init; }
    public required Guid OrganizationId { get; init; }
    
    public required MembershipRole Role { get; init; }
    public bool IsDeleted { get; init; }
    public User User { get; init; } = null!;
    public Organization Organization { get; init; } = null!;
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