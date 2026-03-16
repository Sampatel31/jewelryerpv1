using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data;

/// <summary>
/// Entity Framework Core database context for the Gold Jewellery Management System.
/// Entities and configurations will be added in Phase 2.
/// </summary>
public class GoldDbContext : DbContext
{
    public GoldDbContext(DbContextOptions<GoldDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GoldDbContext).Assembly);
    }
}
