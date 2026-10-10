using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Triage;

/// <summary>
/// The SLA rules that change without a code change. Bound from the "Sla" configuration section.
/// </summary>
public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    /// <summary>The response window per priority, in hours.</summary>
    public Dictionary<TicketPriority, double> BaseWindowHours { get; set; } = [];

    /// <summary>What a Premium customer's window is multiplied by.</summary>
    public double PremiumCustomerMultiplier { get; set; }

    /// <summary>A Premium customer's window is never shorter than this.</summary>
    public double MinimumWindowHours { get; set; }

    /// <summary>A ticket is at risk once this percentage of its window or less remains.</summary>
    public int AtRiskThresholdPercent { get; set; }
}
