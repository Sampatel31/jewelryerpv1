using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data;

/// <summary>
/// Entity Framework Core database context for the Gold Jewellery Management System.
/// </summary>
public class GoldDbContext : DbContext
{
    public GoldDbContext(DbContextOptions<GoldDbContext> options)
        : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<GoldRate> GoldRates => Set<GoldRate>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<OldGoldExchange> OldGoldExchanges => Set<OldGoldExchange>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SyncQueue> SyncQueues => Set<SyncQueue>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GoldDbContext).Assembly);

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default owner branch
        // SqlConnectionString must be configured via application settings before use.
        modelBuilder.Entity<Branch>().HasData(new Branch
        {
            BranchId = 1,
            Code = "HO",
            Name = "Head Office",
            Address = "Head Office Address",
            GSTIN = "000000000000000",
            Phone = "0000000000",
            IsOwnerBranch = true,
            SqlConnectionString = "CONFIGURE_BEFORE_USE",
            IsActive = true
        });

        // Seed default categories
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Ring",     DefaultMakingType = "PERCENT", DefaultMakingValue = 12m, DefaultWastagePercent = 2m, DefaultPurity = "22K", HUIDRequired = true,  SortOrder = 1, IsActive = true },
            new Category { CategoryId = 2, Name = "Chain",    DefaultMakingType = "PERCENT", DefaultMakingValue = 10m, DefaultWastagePercent = 2m, DefaultPurity = "22K", HUIDRequired = true,  SortOrder = 2, IsActive = true },
            new Category { CategoryId = 3, Name = "Bangle",   DefaultMakingType = "PERCENT", DefaultMakingValue = 12m, DefaultWastagePercent = 2m, DefaultPurity = "22K", HUIDRequired = true,  SortOrder = 3, IsActive = true },
            new Category { CategoryId = 4, Name = "Earring",  DefaultMakingType = "PERCENT", DefaultMakingValue = 15m, DefaultWastagePercent = 2m, DefaultPurity = "22K", HUIDRequired = true,  SortOrder = 4, IsActive = true },
            new Category { CategoryId = 5, Name = "Pendant",  DefaultMakingType = "PERCENT", DefaultMakingValue = 12m, DefaultWastagePercent = 2m, DefaultPurity = "22K", HUIDRequired = true,  SortOrder = 5, IsActive = true },
            new Category { CategoryId = 6, Name = "Necklace", DefaultMakingType = "PERCENT", DefaultMakingValue = 10m, DefaultWastagePercent = 2m, DefaultPurity = "22K", HUIDRequired = true,  SortOrder = 6, IsActive = true },
            new Category { CategoryId = 7, Name = "Kada",     DefaultMakingType = "PERCENT", DefaultMakingValue = 10m, DefaultWastagePercent = 2m, DefaultPurity = "22K", HUIDRequired = false, SortOrder = 7, IsActive = true }
        );

        // Seed default users.
        // PasswordHash is set to a sentinel value; the application MUST enforce a
        // password change on first login and replace this with a proper BCrypt hash.
        modelBuilder.Entity<User>().HasData(
            new User { UserId = 1, Name = "System Admin",   Username = "admin",    PasswordHash = "CHANGE_ON_FIRST_LOGIN", Role = "Admin",    BranchId = 1, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { UserId = 2, Name = "Branch Manager", Username = "manager",  PasswordHash = "CHANGE_ON_FIRST_LOGIN", Role = "Manager",  BranchId = 1, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { UserId = 3, Name = "Sales Operator", Username = "operator", PasswordHash = "CHANGE_ON_FIRST_LOGIN", Role = "Operator", BranchId = 1, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
