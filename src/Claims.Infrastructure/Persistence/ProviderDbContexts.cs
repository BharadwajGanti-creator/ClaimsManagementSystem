using Claims.Application.Abstractions;
using Claims.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Claims.Infrastructure.Persistence;

public sealed class SqliteClaimsDbContext(DbContextOptions<SqliteClaimsDbContext> options, IDateTimeProvider clock)
    : ClaimsDbContext(options, clock);

public sealed class SqlServerClaimsDbContext(DbContextOptions<SqlServerClaimsDbContext> options, IDateTimeProvider clock)
    : ClaimsDbContext(options, clock);

public sealed class SqliteClaimsDbContextFactory : IDesignTimeDbContextFactory<SqliteClaimsDbContext>
{
    public SqliteClaimsDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<SqliteClaimsDbContext>().UseSqlite(
            Environment.GetEnvironmentVariable("ConnectionStrings__ClaimsDb") ?? "Data Source=claims.db").Options,
        new SystemDateTimeProvider());
}

public sealed class SqlServerClaimsDbContextFactory : IDesignTimeDbContextFactory<SqlServerClaimsDbContext>
{
    public SqlServerClaimsDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<SqlServerClaimsDbContext>().UseSqlServer(
            Environment.GetEnvironmentVariable("ConnectionStrings__ClaimsDb")
            ?? "Server=localhost;Database=ClaimsDb;Integrated Security=True;TrustServerCertificate=True").Options,
        new SystemDateTimeProvider());
}
