using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Triage;

/// <summary>What triage decided for a ticket, with the reason for each part of it.</summary>
public sealed record TriageDecision(
    TicketPriority Priority,
    string PriorityReason,
    SlaWindow Window,
    DateTime DueAtUtc,
    Assignment Assignment);

/// <summary>
/// Decides priority, due date and owner by putting the individual rules together. Holds no
/// rules of its own, so each of them stays small and testable on its own.
/// </summary>
public sealed class TicketTriage(SlaCalculator sla, AgentSelector agents)
{
    public TriageDecision ForNewTicket(
        Category category,
        CustomerTier tier,
        TicketPriority? requested,
        DateTime nowUtc,
        IEnumerable<AgentWorkload> workloads)

    {
        var priority = category.ResolvePriority(requested);
        var window = sla.WindowFor(priority, tier);

        return new TriageDecision(
            priority,
            PriorityReason(category, requested, priority),
            window,
            window.DueAt(nowUtc),
            agents.Choose(workloads, category));
    }

    /// <summary>
    /// The outcome of escalating a ticket to <paramref name="newPriority"/>: a fresh window from
    /// now, and the current agent kept while they are still eligible.
    /// </summary>
    public TriageDecision ForEscalation(
        Category category,
        CustomerTier tier,
        TicketPriority newPriority,
        int? currentAgentId,
        DateTime nowUtc,
        IEnumerable<AgentWorkload> workloads)
    {
        var window = sla.WindowFor(newPriority, tier);

        return new TriageDecision(
            newPriority,
            $"Escalated one level, to {newPriority}.",
            window,
            window.DueAt(nowUtc),
            agents.KeepOrChoose(currentAgentId, workloads, category));
    }

    private static string PriorityReason(Category category, TicketPriority? requested, TicketPriority priority)
    {
        if (category.ForcesCriticalPriority)
        {
            return $"{category.Name} tickets are always Critical.";
        }

        return requested is null
            ? "No priority was requested, so it defaults to Medium."
            : $"{priority} priority, as requested.";
    }
}
