using Microsoft.EntityFrameworkCore;

namespace SpendLensDatabase;

public class SpendLensDbContext(DbContextOptions<SpendLensDbContext> options)
    : DbContext(options)
{
    public const string OptionName = "DefaultConnection";
    public const string PublicSchema = "public";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(OptionName);
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SpendLensDbContext).Assembly);
    }
}