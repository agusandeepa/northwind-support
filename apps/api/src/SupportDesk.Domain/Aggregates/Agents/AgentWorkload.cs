namespace SupportDesk.Domain.Aggregates.Agents;

/// <summary>An agent and how many open tickets they hold right now.</summary>
public sealed record AgentWorkload(Agent Agent, int OpenTickets);
