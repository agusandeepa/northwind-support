using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>Body of POST /api/tickets/{id}/escalate.</summary>
public sealed record EscalateTicketRequest(string Reason, string EscalatedBy);

/// <summary>One row of a ticket's escalation history: before, after, who and why.</summary>
public sealed class EscalationDto
{
    public int Id { get; set; }

    public TicketPriority FromPriority { get; set; }

    public TicketPriority ToPriority { get; set; }

    public AgentSummaryDto? FromAgent { get; set; }

    public AgentSummaryDto? ToAgent { get; set; }

    public DateTime? FromDueAtUtc { get; set; }

    public DateTime? ToDueAtUtc { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string EscalatedBy { get; set; } = string.Empty;

    public DateTime EscalatedAtUtc { get; set; }
}

/// <summary>The response to an escalation: the ticket as it now stands, and the row that was recorded.</summary>
public sealed record EscalationResultDto(TicketDetailDto Ticket, EscalationDto Escalation);
