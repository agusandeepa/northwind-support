using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;

namespace SupportDesk.Application.Triage;

/// <summary>An agent and how many open tickets they hold right now.</summary>
public sealed record AgentWorkload(Agent Agent, int OpenTickets);

/// <summary>Who a ticket goes to (null for nobody) and why.</summary>
public sealed record Assignment(Agent? Agent, string Reason);

/// <summary>
/// Chooses who owns a ticket. Shared by creating and escalating a ticket. Having nobody
/// eligible is a normal outcome, not an error: the ticket is simply left unassigned.
/// </summary>
public sealed class AgentSelector
{
    /// <summary>
    /// The eligible agent with the fewest open tickets. Ties go to the lowest id, so the same
    /// input always gives the same answer.
    /// </summary>
    public Assignment Choose(IEnumerable<AgentWorkload> workloads, Category category)
    {
        var all = workloads.ToList();

        var eligible = all
            .Where(w => w.Agent.CanTake(category, w.OpenTickets))
            .OrderBy(w => w.OpenTickets)
            .ThenBy(w => w.Agent.Id)
            .ToList();

        if (eligible.Count == 0)
        {
            return new Assignment(null, WhyNobody(all, category));
        }

        var chosen = eligible[0];

        return new Assignment(
            chosen.Agent,
            $"Assigned to {chosen.Agent.FullName}: fewest open tickets ({chosen.OpenTickets}) "
            + $"among {eligible.Count} eligible agent(s).");
    }

    /// <summary>
    /// Keeps the current agent while they are still eligible, otherwise chooses again.
    /// </summary>
    public Assignment KeepOrChoose(int? currentAgentId, IEnumerable<AgentWorkload> workloads, Category category)
    {
        var all = workloads.ToList();

        var current = currentAgentId is null ? null : all.FirstOrDefault(w => w.Agent.Id == currentAgentId);

        // The ticket being re-checked is one of the current agent's own open tickets, so it
        // must not count against their limit.
        if (current is not null && current.Agent.CanTake(category, current.OpenTickets - 1))
        {
            return new Assignment(current.Agent, $"Kept with {current.Agent.FullName}: still eligible.");
        }

        return Choose(all, category);
    }

    private static string WhyNobody(List<AgentWorkload> all, Category category)
    {
        var qualified = all.Count(w =>
            w.Agent.IsActive && (!category.RequiresSpecialist || w.Agent.HasSpecialization(category.Id)));

        if (qualified > 0)
        {
            return "Left unassigned: every eligible agent is at their open-ticket limit.";
        }

        return category.RequiresSpecialist
            ? $"Left unassigned: no active agent specialises in {category.Name}."
            : "Left unassigned: there is no active agent.";
    }
}
