using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Infrastructure.Data;

/// <summary>
/// Gives every seeded agent a password, hashed here at seed time. Development only: it is
/// run by the same startup step that seeds the demo data, and never in a real deployment.
/// </summary>
public sealed class DevelopmentCredentials(
    SupportDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<DevelopmentCredentials> logger)
{
    /// <summary>
    /// The password of every seeded agent. An obvious local fixture, not a secret, documented
    /// in the README.
    /// </summary>
    public const string Password = "LocalDevOnly!1";

    /// <summary>Sets the development password on agents that have none, so an old database works too.</summary>
    public async Task EnsureAsync(CancellationToken ct = default)
    {
        var agents = await db.Set<Agent>().Where(a => a.PasswordHash == null).ToListAsync(ct);

        if (agents.Count == 0)
        {
            return;
        }

        foreach (var agent in agents)
        {
            agent.SetPasswordHash(passwordHasher.Hash(Password));
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Set the development password on {Count} agent(s).", agents.Count);
    }
}
