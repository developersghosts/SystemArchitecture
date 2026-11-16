using Microsoft.EntityFrameworkCore.Design;

namespace ServicesA.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools (dotnet ef) at design time only, so migrations don't need to build the whole API host.
/// Connection string: ConnectionStrings__DefaultConnection environment variable, or LocalDB as a fallback.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string FallbackConnection =
        @"Server=(localdb)\MSSQLLocalDB;Database=ServicesA;Trusted_Connection=True;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                               ?? FallbackConnection;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
