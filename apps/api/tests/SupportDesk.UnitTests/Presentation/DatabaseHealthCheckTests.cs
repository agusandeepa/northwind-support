using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Presentation.Health;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Presentation;

public class DatabaseHealthCheckTests
{
    private static Task<HealthCheckResult> Check(SupportDbContext db) =>
        new DatabaseHealthCheck(db).CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

    [Fact]
    public async Task AReachableDatabase_IsHealthy()
    {
        using var database = new SqliteTestDatabase();
        using var db = database.CreateContext();

        Assert.Equal(HealthStatus.Healthy, (await Check(db)).Status);
    }

    [Fact]
    public async Task AnUnreachableDatabase_IsUnhealthy()
    {
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite("Data Source=/no/such/folder/support.db;Mode=ReadOnly")
            .Options;
        using var db = new SupportDbContext(options);

        Assert.Equal(HealthStatus.Unhealthy, (await Check(db)).Status);
    }
}
