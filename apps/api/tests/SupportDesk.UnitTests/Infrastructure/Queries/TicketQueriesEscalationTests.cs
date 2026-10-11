using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

public sealed class TicketQueriesEscalationTests : IDisposable
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    private readonly SqliteTestDatabase _database = new();
    private readonly int _ticketId;

    public TicketQueriesEscalationTests()
    {
        using var db = _database.CreateContext();

        var customer = new Customer("Contoso", "help@contoso.test", null, CustomerTier.Standard, Now);
        var category = new Category("General", false, false);
        var alex = new Agent("Alex Turner", "alex@test.example", 10, Now);
        db.AddRange(customer, category, alex);
        db.SaveChanges();

        var ticket = Ticket.Raise("TCK-0001", "A title", "A description", customer.Id, category.Id, TicketPriority.Low, Now);
        db.Add(ticket);
        db.SaveChanges();
        _ticketId = ticket.Id;

        db.Add(new TicketEscalation(
            _ticketId, TicketPriority.Low, TicketPriority.Medium, null, alex.Id, null, Now.AddHours(24),
            "First", "alex@test.example", Now.AddHours(1)));
        db.Add(new TicketEscalation(
            _ticketId, TicketPriority.Medium, TicketPriority.High, alex.Id, null, Now.AddHours(24), Now.AddHours(10),
            "Second", "alex@test.example", Now.AddHours(2)));
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetEscalationsAsync_ReturnsTheHistoryNewestFirstWithAgentNames()
    {
        using var db = _database.CreateContext();

        var history = await new TicketQueries(db, new FixedClock(), TestSlaOptions.Create()).GetEscalationsAsync(_ticketId, CancellationToken.None);

        Assert.NotNull(history);
        Assert.Equal(new[] { "Second", "First" }, history.Select(e => e.Reason).ToArray());

        Assert.Equal("Alex Turner", history[0].FromAgent!.FullName);
        Assert.Null(history[0].ToAgent);
        Assert.Null(history[1].FromAgent);
        Assert.Equal("Alex Turner", history[1].ToAgent!.FullName);
        Assert.Equal(TicketPriority.High, history[0].ToPriority);
    }

    [Fact]
    public async Task GetEscalationsAsync_ForAnUnknownTicket_ReturnsNull()
    {
        using var db = _database.CreateContext();

        Assert.Null(await new TicketQueries(db, new FixedClock(), TestSlaOptions.Create()).GetEscalationsAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task GetEscalationsAsync_ForATicketWithNoHistory_ReturnsAnEmptyList()
    {
        using var db = _database.CreateContext();
        var other = Ticket.Raise("TCK-0002", "Another", "Another description", 1, 1, TicketPriority.Low, Now);
        db.Add(other);
        db.SaveChanges();

        var history = await new TicketQueries(db, new FixedClock(), TestSlaOptions.Create()).GetEscalationsAsync(other.Id, CancellationToken.None);

        Assert.NotNull(history);
        Assert.Empty(history);
    }
}
