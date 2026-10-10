using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class TicketEscalateTests
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    [Theory]
    [InlineData(TicketPriority.Low, TicketPriority.Medium)]
    [InlineData(TicketPriority.Medium, TicketPriority.High)]
    [InlineData(TicketPriority.High, TicketPriority.Critical)]
    public void NextEscalationPriority_IsOneLevelUp(TicketPriority current, TicketPriority expected) =>
        Assert.Equal(expected, new TicketBuilder().WithPriority(current).Build().NextEscalationPriority());

    [Fact]
    public void NextEscalationPriority_ForACriticalTicket_SaysSo()
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => new TicketBuilder().WithPriority(TicketPriority.Critical).Build().NextEscalationPriority());

        Assert.Contains("Critical", exception.Message);
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public void NextEscalationPriority_ForAFinishedTicket_NamesTheStatus(TicketStatus status)
    {
        var exception = Assert.Throws<BusinessRuleViolationException>(
            () => new TicketBuilder().WithStatus(status).Build().NextEscalationPriority());

        Assert.Contains(status.ToString(), exception.Message);
    }

    [Fact]
    public void Escalate_RaisesThePriorityRestartsTheSlaAndMovesTheOwner()
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Medium).AssignedTo(3).Build();
        ticket.StartSla(Now, Now.AddHours(24));
        var later = Now.AddHours(2);

        var escalation = ticket.Escalate(later.AddHours(8), 5, "Customer is blocked", "alex@test.example", later);

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(later, ticket.SlaStartedAtUtc);
        Assert.Equal(later.AddHours(8), ticket.DueAtUtc);
        Assert.Equal(5, ticket.AssignedAgentId);

        Assert.Equal(TicketPriority.Medium, escalation.FromPriority);
        Assert.Equal(TicketPriority.High, escalation.ToPriority);
        Assert.Equal(3, escalation.FromAgentId);
        Assert.Equal(5, escalation.ToAgentId);
        Assert.Equal(Now.AddHours(24), escalation.FromDueAtUtc);
        Assert.Equal(later.AddHours(8), escalation.ToDueAtUtc);
        Assert.Equal("Customer is blocked", escalation.Reason);
        Assert.Equal("alex@test.example", escalation.EscalatedBy);
        Assert.Equal(later, escalation.EscalatedAtUtc);
    }

    [Fact]
    public void Escalate_ACriticalTicket_ChangesNothing()
    {
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Critical).Build();

        Assert.Throws<BusinessRuleViolationException>(
            () => ticket.Escalate(Now.AddHours(4), null, "A good reason", "alex@test.example", Now));

        Assert.Equal(TicketPriority.Critical, ticket.Priority);
        Assert.Null(ticket.DueAtUtc);
    }
}
