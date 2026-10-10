using SupportDesk.Application.Triage;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.TestDoubles;

internal static class TestSlaOptions
{
    /// <summary>The SLA rules as configured in appsettings.Development.json.</summary>
    public static SlaOptions Create(double multiplier = 0.5, double minimumHours = 1) => new()
    {
        BaseWindowHours = new Dictionary<TicketPriority, double>
        {
            [TicketPriority.Low] = 72,
            [TicketPriority.Medium] = 24,
            [TicketPriority.High] = 8,
            [TicketPriority.Critical] = 4
        },
        PremiumCustomerMultiplier = multiplier,
        MinimumWindowHours = minimumHours,
        AtRiskThresholdPercent = 25
    };
}
