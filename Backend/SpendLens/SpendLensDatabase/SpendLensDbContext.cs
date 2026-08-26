using Microsoft.EntityFrameworkCore;
using SpendLensDatabase.Models;
using SpendLensDatabase.Models.Auth;
using SpendLensDatabase.Models.Auth.Organization;
using SpendLensDatabase.Models.Auth.Users;

namespace SpendLensDatabase;

public class SpendLensDbContext(DbContextOptions<SpendLensDbContext> options)
    : DbContext(options)
{
    public const string OptionName = "DefaultConnection";
    public const string PublicSchema = "public";
    
    public DbSet<Membership>  Memberships { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(OptionName);
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SpendLensDbContext).Assembly);
    }
}