using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class TicketSlaTests
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    private static Ticket ATicket() =>
        Ticket.Raise("TCK-0001", "A title", "A description", 1, 1, TicketPriority.Medium, Now);

    [Fact]
    public void Raise_StartsTheSlaClockAtCreation() =>
        Assert.Equal(Now, ATicket().SlaStartedAtUtc);

    [Fact]
    public void StartSla_SetsTheWindow()
    {
        var ticket = ATicket();

        ticket.StartSla(Now.AddHours(1), Now.AddHours(5));

        Assert.Equal(Now.AddHours(1), ticket.SlaStartedAtUtc);
        Assert.Equal(Now.AddHours(5), ticket.DueAtUtc);
        Assert.Equal(Now.AddHours(1), ticket.UpdatedAtUtc);
    }

    [Fact]
    public void StartSla_WithADueDateBeforeTheStart_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ATicket().StartSla(Now.AddHours(2), Now.AddHours(1)));
}
