using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Domain.Repositories;

/// <summary>
/// Loads <see cref="Agent"/> aggregates.
/// </summary>
public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Every agent, with the number of open tickets each one holds.</summary>
    Task<IReadOnlyList<AgentWorkload>> GetWorkloadsAsync(CancellationToken ct);
        /// <summary>
    /// The agent with this email address, or null. Used to sign in; the lookup is untracked
    /// because signing in never changes the agent.
    /// </summary>
    Task<Agent?> GetByEmailAsync(string email, CancellationToken ct);

}

