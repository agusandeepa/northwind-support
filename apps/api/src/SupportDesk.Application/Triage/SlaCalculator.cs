using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Triage;

/// <summary>How long a ticket has to be answered, and why that long.</summary>
public sealed record SlaWindow(TimeSpan Duration, string Reason)
{
    public DateTime DueAt(DateTime startUtc) => startUtc + Duration;
}

/// <summary>
/// Works out the response window for a priority and a customer tier. Shared by creating and
/// escalating a ticket, so the two can never disagree.
/// </summary>
public sealed class SlaCalculator(SlaOptions options)
{
    /// <exception cref="InvalidOperationException">No window is configured for the priority.</exception>
    public SlaWindow WindowFor(TicketPriority priority, CustomerTier tier)
    {
        if (!options.BaseWindowHours.TryGetValue(priority, out var baseHours))
        {
            throw new InvalidOperationException(
                $"No SLA window is configured for {priority} priority (Sla:BaseWindowHours).");
        }

        var standard = $"{priority} priority: {Hours(baseHours)} base window";

        if (tier != CustomerTier.Premium)
        {
            return new SlaWindow(TimeSpan.FromHours(baseHours), $"{standard}.");
        }

        var reduced = baseHours * options.PremiumCustomerMultiplier;
        var hours = Math.Max(reduced, options.MinimumWindowHours);

        var reason = hours > reduced
            ? $"{standard} x {Number(options.PremiumCustomerMultiplier)} for a Premium customer = {Hours(reduced)}, "
              + $"raised to the {Hours(options.MinimumWindowHours)} minimum."
            : $"{standard} x {Number(options.PremiumCustomerMultiplier)} for a Premium customer = {Hours(hours)}.";

        return new SlaWindow(TimeSpan.FromHours(hours), reason);
    }

    private static string Number(double value) => FormattableString.Invariant($"{value:0.##}");

    private static string Hours(double hours) => $"{Number(hours)} h";
}
