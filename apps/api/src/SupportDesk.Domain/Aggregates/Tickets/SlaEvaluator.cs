namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored. The current time is a parameter,
/// so a test can put it exactly on a boundary.
/// </summary>
public static class SlaEvaluator
{
    /// <param name="slaStartedAtUtc">When the current window started.</param>
    /// <param name="dueAtUtc">When the window ends; null when no SLA applies.</param>
    /// <param name="resolvedAtUtc">When the ticket was resolved; null while it is open.</param>
    /// <param name="nowUtc">The current time.</param>
    /// <param name="atRiskThresholdPercent">An open ticket is at risk once this percentage of its window or less is left.</param>
    public static SlaStatus Evaluate(
        DateTime slaStartedAtUtc,
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime nowUtc,
        int atRiskThresholdPercent)
    {
        if (dueAtUtc is not { } due)
        {
            return SlaStatus.NotApplicable;
        }

        if (resolvedAtUtc is { } resolved)
        {
            return resolved <= due ? SlaStatus.Met : SlaStatus.Breached;
        }

        if (nowUtc > due)
        {
            return SlaStatus.Breached;
        }

        var window = due - slaStartedAtUtc;
        var remaining = due - nowUtc;

        // remaining / window <= percent / 100, kept in whole ticks so there is no rounding.
        return remaining.Ticks * 100 <= window.Ticks * atRiskThresholdPercent
            ? SlaStatus.AtRisk
            : SlaStatus.WithinSla;
    }
}
