namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>Why a new ticket got the priority, due date and owner it did, in plain words.</summary>
public sealed record TriageDto(string PriorityReason, string SlaReason, string AssignmentReason);

/// <summary>The response to raising a ticket: the ticket as it now stands, plus how it was triaged.</summary>
public sealed class TicketCreatedDto : TicketDetailDto
{
    public TriageDto Triage { get; set; } = null!;

    public static TicketCreatedDto From(TicketDetailDto ticket, TriageDto triage) => new()
    {
        Id = ticket.Id,
        Reference = ticket.Reference,
        Title = ticket.Title,
        Description = ticket.Description,
        Status = ticket.Status,
        Priority = ticket.Priority,
        Customer = ticket.Customer,
        Category = ticket.Category,
        AssignedAgent = ticket.AssignedAgent,
        CreatedAtUtc = ticket.CreatedAtUtc,
        UpdatedAtUtc = ticket.UpdatedAtUtc,
        DueAtUtc = ticket.DueAtUtc,
        ResolvedAtUtc = ticket.ResolvedAtUtc,
        SlaStatus = ticket.SlaStatus,
        Triage = triage
    };
}
