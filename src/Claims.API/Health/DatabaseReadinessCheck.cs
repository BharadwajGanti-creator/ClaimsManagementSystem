using Claims.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Claims.API.Health;

public sealed class DatabaseReadinessCheck(IServiceScopeFactory scopes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopes.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        try
        {
            return await database.Database.CanConnectAsync(cancellationToken)
                && !(await database.Database.GetPendingMigrationsAsync(cancellationToken)).Any()
                ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database is not ready.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database is not ready.");
        }
    }
}
