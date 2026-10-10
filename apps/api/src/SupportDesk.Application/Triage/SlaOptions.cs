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

    /// <summary>Fails at startup, rather than on the first ticket, when the configuration is unusable.</summary>
    /// <exception cref="InvalidOperationException">A value is missing or out of range.</exception>
    public SlaOptions EnsureValid()
    {
        foreach (var priority in Enum.GetValues<TicketPriority>())
        {
            if (!BaseWindowHours.TryGetValue(priority, out var hours) || hours <= 0)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:BaseWindowHours:{priority} must be a positive number of hours.");
            }
        }

        if (PremiumCustomerMultiplier is <= 0 or > 1)
        {
            throw new InvalidOperationException($"{SectionName}:PremiumCustomerMultiplier must be above 0 and at most 1.");
        }

        if (MinimumWindowHours < 0)
        {
            throw new InvalidOperationException($"{SectionName}:MinimumWindowHours cannot be negative.");
        }

        if (AtRiskThresholdPercent is < 0 or > 100)
        {
            throw new InvalidOperationException($"{SectionName}:AtRiskThresholdPercent must be between 0 and 100.");
        }

        return this;
    }
}
