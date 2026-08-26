using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpendLensDatabase.Models.Auth.Users;

namespace SpendLensDatabase.Models.Auth;

public class RefreshToken
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required byte[] TokenHash { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required DateTime? RevokedAt { get; init; }
    public User User { get; init; } = null!;
}

file sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", SpendLensDbContext.PublicSchema);
        
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.TokenHash)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(r => r.ExpiresAt)
            .IsRequired()
            .HasConversion(dw => DateTime.SpecifyKind(dw, DateTimeKind.Utc), dr => dr);

        builder.Property(r => r.RevokedAt)
            .HasConversion(dw => dw == null
                    ? (DateTime?)null
                    : DateTime.SpecifyKind(dw.Value,
                        DateTimeKind.Utc),
                dr => dr);
        
       builder.HasOne(r => r.User)
           .WithMany()
           .HasForeignKey(r => r.UserId)
           .OnDelete(DeleteBehavior.NoAction);
    }
}