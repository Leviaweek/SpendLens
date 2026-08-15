using Microsoft.EntityFrameworkCore;

namespace SpendLensDatabase;

public class SpendLensDbContext(DbContextOptions<SpendLensDbContext> options)
    : DbContext(options)
{
    public const string OptionName = "DefaultConnection";
}