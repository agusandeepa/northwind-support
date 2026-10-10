using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public class EscalateTicketCommandHandlerTests
{
    private static readonly EscalateTicketRequest Request = new("  Customer is blocked  ", " alex@test.example ");

    private static Agent AnAgent(int id, bool active = true)
    {
        var agent = new Agent($"Agent {id}", $"agent{id}@test.example", 10, FixedClock.DefaultNow).WithId(id);

        if (!active)
        {
            agent.Deactivate();
        }

        return agent;
    }

    /// <summary>A Medium ticket, created at the fixed time with a 24 h window, held by agent 7.</summary>
    private static Ticket AMediumTicketHeldBy(int agentId)
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Medium).AssignedTo(agentId).Build();
        ticket.StartSla(FixedClock.DefaultNow, FixedClock.DefaultNow.AddHours(24));

        return ticket;
    }

    [Fact]
    public async Task HandleAsync_RaisesOneLevelAndMeasuresTheNewWindowFromNow()
    {
        var context = new TicketCommandTestContext();
        var ticket = context.ExistingTicket(AMediumTicketHeldBy(7));
        context.WithWorkloads(new AgentWorkload(AnAgent(7), 5));
        context.Clock.Advance(TimeSpan.FromHours(3));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request, CancellationToken.None);

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(context.Clock.UtcNow, ticket.SlaStartedAtUtc);
        Assert.Equal(context.Clock.UtcNow.AddHours(8), ticket.DueAtUtc);
        context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task HandleAsync_ForAPremiumCustomer_UsesTheHalvedWindow()
    {
        var context = new TicketCommandTestContext();
        context.ExistingCustomer(CustomerTier.Premium);
        var ticket = context.ExistingTicket(AMediumTicketHeldBy(7));
        context.WithWorkloads(new AgentWorkload(AnAgent(7), 5));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request, CancellationToken.None);

        Assert.Equal(context.Clock.UtcNow.AddHours(4), ticket.DueAtUtc);
    }

    [Fact]
    public async Task HandleAsync_KeepsTheCurrentAgentWhileTheyAreStillEligible()
    {
        var context = new TicketCommandTestContext();
        var ticket = context.ExistingTicket(AMediumTicketHeldBy(7));
        context.WithWorkloads(new AgentWorkload(AnAgent(7), 9), new AgentWorkload(AnAgent(8), 0));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request, CancellationToken.None);

        Assert.Equal(7, ticket.AssignedAgentId);
        Assert.NotNull(context.AddedEscalation);
        Assert.Equal(7, context.AddedEscalation.FromAgentId);
        Assert.Equal(7, context.AddedEscalation.ToAgentId);
    }

    [Fact]
    public async Task HandleAsync_ReassignsWhenTheCurrentAgentIsNoLongerEligible()
    {
        var context = new TicketCommandTestContext();
        var ticket = context.ExistingTicket(AMediumTicketHeldBy(7));
        context.WithWorkloads(new AgentWorkload(AnAgent(7, active: false), 2), new AgentWorkload(AnAgent(8), 4));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request, CancellationToken.None);

        Assert.Equal(8, ticket.AssignedAgentId);
        Assert.NotNull(context.AddedEscalation);
        Assert.Equal(7, context.AddedEscalation.FromAgentId);
        Assert.Equal(8, context.AddedEscalation.ToAgentId);
    }

    [Fact]
    public async Task HandleAsync_WhenNobodyIsEligible_LeavesTheTicketUnassigned()
    {
        var context = new TicketCommandTestContext();
        var ticket = context.ExistingTicket(AMediumTicketHeldBy(7));
        context.WithWorkloads(new AgentWorkload(AnAgent(7, active: false), 2));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request, CancellationToken.None);

        Assert.Null(ticket.AssignedAgentId);
        Assert.NotNull(context.AddedEscalation);
        Assert.Null(context.AddedEscalation.ToAgentId);
    }

    [Fact]
    public async Task HandleAsync_RecordsTheHistoryRow()
    {
        var context = new TicketCommandTestContext();
        var ticket = context.ExistingTicket(AMediumTicketHeldBy(7));
        context.WithWorkloads(new AgentWorkload(AnAgent(7), 5));
        context.Clock.Advance(TimeSpan.FromHours(3));

        await context.EscalateTicket.HandleAsync(ticket.Id, Request, CancellationToken.None);

        var row = context.AddedEscalation;
        Assert.NotNull(row);
        Assert.Equal(TicketPriority.Medium, row.FromPriority);
        Assert.Equal(TicketPriority.High, row.ToPriority);
        Assert.Equal(FixedClock.DefaultNow.AddHours(24), row.FromDueAtUtc);
        Assert.Equal(context.Clock.UtcNow.AddHours(8), row.ToDueAtUtc);
        Assert.Equal("Customer is blocked", row.Reason);
        Assert.Equal("alex@test.example", row.EscalatedBy);
        Assert.Equal(context.Clock.UtcNow, row.EscalatedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ForACriticalTicket_IsRefusedAndNothingIsSaved()
    {
        var context = new TicketCommandTestContext();
        var ticket = context.ExistingTicket(new TicketBuilder().WithPriority(TicketPriority.Critical).Build());

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => context.EscalateTicket.HandleAsync(ticket.Id, Request, CancellationToken.None));

        Assert.Contains("Critical", exception.Message);
        Assert.Null(context.AddedEscalation);
        context.VerifySaved(Times.Never());
    }

    [Fact]
    public async Task HandleAsync_ForAResolvedTicket_IsRefusedAndSaysWhy()
    {
        var context = new TicketCommandTestContext();
        var ticket = context.ExistingTicket(new TicketBuilder().WithStatus(TicketStatus.Resolved).Build());

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => context.EscalateTicket.HandleAsync(ticket.Id, Request, CancellationToken.None));

        Assert.Contains("Resolved", exception.Message);
        context.VerifySaved(Times.Never());
    }

    [Fact]
    public async Task HandleAsync_ForAnUnknownTicket_Throws()
    {
        var context = new TicketCommandTestContext();

        await Assert.ThrowsAsync<NotFoundException>(
            () => context.EscalateTicket.HandleAsync(999, Request, CancellationToken.None));

        context.VerifySaved(Times.Never());
    }
}
