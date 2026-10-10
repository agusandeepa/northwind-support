using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Triage;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public class RaiseTicketCommandHandlerTests
{
    private static CreateTicketRequest Request(TicketPriority? priority = null) =>
        new("Cannot log in to the portal", "Happens for every user since this morning.", 1, 3, priority);

    [Fact]
    public async Task HandleAsync_StampsReferenceStatusAndTimestamps()
    {
        var context = new TicketCommandTestContext();

        await context.RaiseTicket.HandleAsync(Request(TicketPriority.High), CancellationToken.None);

        var created = context.AddedTicket;
        Assert.NotNull(created);
        Assert.Equal("TCK-0041", created.Reference);
        Assert.Equal(TicketStatus.New, created.Status);
        Assert.Equal(TicketPriority.High, created.Priority);
        Assert.Equal(FixedClock.DefaultNow, created.CreatedAtUtc);
        Assert.Equal(FixedClock.DefaultNow, created.UpdatedAtUtc);
        context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task HandleAsync_WithoutRequestedPriority_FallsBackToMedium()
    {
        var context = new TicketCommandTestContext();

        await context.RaiseTicket.HandleAsync(Request(), CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(TicketPriority.Medium, context.AddedTicket.Priority);
    }

    [Fact]
    public async Task HandleAsync_StartsTheSlaClockAndSetsTheDueDate()
    {
        var context = new TicketCommandTestContext();

        await context.RaiseTicket.HandleAsync(Request(TicketPriority.High), CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(FixedClock.DefaultNow, context.AddedTicket.SlaStartedAtUtc);
        Assert.Equal(FixedClock.DefaultNow.AddHours(8), context.AddedTicket.DueAtUtc);
    }

    [Fact]
    public async Task HandleAsync_ForAPremiumCustomer_HalvesTheWindow()
    {
        var context = new TicketCommandTestContext();
        context.ExistingCustomer(CustomerTier.Premium);

        await context.RaiseTicket.HandleAsync(Request(TicketPriority.High), CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(FixedClock.DefaultNow.AddHours(4), context.AddedTicket.DueAtUtc);
    }

    [Fact]
    public async Task HandleAsync_InACategoryThatForcesCritical_IgnoresTheRequestedPriority()
    {
        var context = new TicketCommandTestContext();
        context.ExistingCategory("Outage", forcesCriticalPriority: true);

        var result = await context.RaiseTicket.HandleAsync(Request(TicketPriority.Low), CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(TicketPriority.Critical, context.AddedTicket.Priority);
        Assert.Equal(FixedClock.DefaultNow.AddHours(4), context.AddedTicket.DueAtUtc);
        Assert.Contains("always Critical", result.Triage.PriorityReason);
    }

    [Fact]
    public async Task HandleAsync_AssignsTheEligibleAgentWithTheFewestOpenTickets()
    {
        var context = new TicketCommandTestContext();
        var busy = new Agent("Busy Agent", "busy@test.example", 10, FixedClock.DefaultNow).WithId(7);
        var free = new Agent("Free Agent", "free@test.example", 10, FixedClock.DefaultNow).WithId(8);
        context.WithWorkloads(new AgentWorkload(busy, 6), new AgentWorkload(free, 1));

        var result = await context.RaiseTicket.HandleAsync(Request(), CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(8, context.AddedTicket.AssignedAgentId);
        Assert.Contains("Free Agent", result.Triage.AssignmentReason);
    }

    [Fact]
    public async Task HandleAsync_WhenNobodyIsEligible_StillCreatesTheTicketUnassigned()
    {
        var context = new TicketCommandTestContext();
        var full = new Agent("Full Agent", "full@test.example", 3, FixedClock.DefaultNow).WithId(7);
        context.WithWorkloads(new AgentWorkload(full, 3));

        var result = await context.RaiseTicket.HandleAsync(Request(), CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Null(context.AddedTicket.AssignedAgentId);
        Assert.StartsWith("Left unassigned", result.Triage.AssignmentReason);
        context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task HandleAsync_ExplainsTheSlaWindow()
    {
        var context = new TicketCommandTestContext();

        var result = await context.RaiseTicket.HandleAsync(Request(TicketPriority.Medium), CancellationToken.None);

        Assert.Equal("Medium priority: 24 h base window.", result.Triage.SlaReason);
    }

    [Fact]
    public async Task HandleAsync_WhenCustomerDoesNotExist_Throws()
    {
        var context = new TicketCommandTestContext();
        context.Customers
            .Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => context.RaiseTicket.HandleAsync(Request(), CancellationToken.None));

        Assert.Contains("Customer", exception.Message);
        context.VerifySaved(Times.Never());
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryDoesNotExist_Throws()
    {
        var context = new TicketCommandTestContext();
        context.Categories
            .Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportDesk.Domain.Aggregates.Categories.Category?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => context.RaiseTicket.HandleAsync(Request(), CancellationToken.None));

        Assert.Contains("Category", exception.Message);
        context.VerifySaved(Times.Never());
    }
}
