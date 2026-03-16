using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GoldSystem.Data;

/// <summary>
/// Design-time factory used by EF Core tools (dotnet ef migrations) to create
/// a GoldDbContext without requiring a running application host.
/// The connection string is resolved from the GOLDSYSTEM_CONNSTR environment variable,
/// falling back to a LocalDB default for local developer machines.
/// This class is never used at runtime.
/// </summary>
public class GoldDbContextFactory : IDesignTimeDbContextFactory<GoldDbContext>
{
    public GoldDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("GOLDSYSTEM_CONNSTR")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=GoldSystemDb;Trusted_Connection=True;";

        var optionsBuilder = new DbContextOptionsBuilder<GoldDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(GoldDbContext).Assembly.GetName().Name));

        return new GoldDbContext(optionsBuilder.Options);
    }
}
