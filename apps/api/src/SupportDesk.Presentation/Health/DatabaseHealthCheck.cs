using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Presentation.Health;

/// <summary>Healthy when the database can be reached. Backs GET /health.</summary>
public sealed class DatabaseHealthCheck(SupportDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy("The database is reachable.")
                : HealthCheckResult.Unhealthy("The database cannot be reached.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The database cannot be reached.", exception);
        }
    }
}
