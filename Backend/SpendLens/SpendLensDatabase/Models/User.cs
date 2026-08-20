using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpendLensDatabase.Models;

public class User
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public required string PasswordHash { get; init; }
    public required DateTime CreatedAt { get; init; }
    public ICollection<Membership> Memberships { get; init; } = [];
}

file sealed class UserConfigure: IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", SpendLensDbContext.PublicSchema);
        
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254);
        
        builder.HasIndex(u => u.Email)
            .IsUnique();
        
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(60);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasConversion(dw => DateTime.SpecifyKind(dw, DateTimeKind.Utc), dw => dw);
    }
}