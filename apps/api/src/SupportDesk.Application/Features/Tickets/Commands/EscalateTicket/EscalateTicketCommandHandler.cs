using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetEscalations;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Triage;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

/// <summary>
/// Escalates a ticket one priority level: new due date, owner re-checked, history recorded.
/// Uses the same triage rules as raising a ticket.
/// </summary>
public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentRepository agents,
    TicketTriage triage,
    IUnitOfWork unitOfWork,
    IClock clock,
    GetTicketQueryHandler getTicket,
    GetEscalationsQueryHandler getEscalations,
    ILogger<EscalateTicketCommandHandler> logger)
{
    /// <exception cref="NotFoundException">The ticket does not exist.</exception>
    /// <exception cref="SupportDesk.Domain.Exceptions.BusinessRuleViolationException">
    /// The ticket is Critical, Resolved or Closed.
    /// </exception>
    public async Task<EscalationResultDto> HandleAsync(int id, EscalateTicketRequest request, CancellationToken ct)
    {
        var ticket = await tickets.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ticket", id);

        // Refuses a Critical, Resolved or Closed ticket before anything else is read.
        var newPriority = ticket.NextEscalationPriority();

        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);

        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);

        var now = clock.UtcNow;

        var decision = triage.ForEscalation(
            category, customer.Tier, newPriority, ticket.AssignedAgentId, now, await agents.GetWorkloadsAsync(ct));

        var escalation = ticket.Escalate(
            decision.DueAtUtc, decision.Assignment.Agent?.Id, request.Reason.Trim(), request.EscalatedBy.Trim(), now);

        await tickets.AddEscalationAsync(escalation, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} escalated to {Priority} by {EscalatedBy}; owner {AgentId}.",
            ticket.Reference, ticket.Priority, escalation.EscalatedBy, ticket.AssignedAgentId);

        var history = await getEscalations.HandleAsync(id, ct);

        return new EscalationResultDto(await getTicket.HandleAsync(id, ct), history[0]);
    }
}
