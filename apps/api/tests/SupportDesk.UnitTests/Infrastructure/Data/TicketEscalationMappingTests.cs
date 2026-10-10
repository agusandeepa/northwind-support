using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Data;

/// <summary>The TicketEscalations table as the model defines it, on in-memory SQLite.</summary>
public sealed class TicketEscalationMappingTests : IDisposable
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    private readonly SqliteTestDatabase _database = new();
    private readonly int _ticketId;

    public TicketEscalationMappingTests()
    {
        using var db = _database.CreateContext();

        var customer = new Customer("Contoso", "help@contoso.test", null, CustomerTier.Standard, Now);
        var category = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
        db.AddRange(customer, category);
        db.SaveChanges();

        var ticket = Ticket.Raise(
            "TCK-0001", "A title", "A description", customer.Id, category.Id, TicketPriority.Medium, Now);
        db.Add(ticket);
        db.SaveChanges();

        _ticketId = ticket.Id;
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void AnEscalation_IsStoredAndReadBackWithEveryField()
    {
        using (var db = _database.CreateContext())
        {
            db.Add(new TicketEscalation(
                _ticketId,
                TicketPriority.Medium,
                TicketPriority.High,
                fromAgentId: null,
                toAgentId: null,
                fromDueAtUtc: Now.AddHours(24),
                toDueAtUtc: Now.AddHours(8),
                reason: "Customer is blocked",
                escalatedBy: "alex@test.example",
                escalatedAtUtc: Now));
            db.SaveChanges();
        }

        using var read = _database.CreateContext();
        var stored = read.Set<TicketEscalation>().Single();

        Assert.Equal(_ticketId, stored.TicketId);
        Assert.Equal(TicketPriority.Medium, stored.FromPriority);
        Assert.Equal(TicketPriority.High, stored.ToPriority);
        Assert.Null(stored.FromAgentId);
        Assert.Null(stored.ToAgentId);
        Assert.Equal(Now.AddHours(24), stored.FromDueAtUtc);
        Assert.Equal(Now.AddHours(8), stored.ToDueAtUtc);
        Assert.Equal("Customer is blocked", stored.Reason);
        Assert.Equal("alex@test.example", stored.EscalatedBy);
        Assert.Equal(Now, stored.EscalatedAtUtc);
    }

    [Fact]
    public void AnEscalation_ThatDoesNotRaiseThePriority_IsRejectedByTheDatabase()
    {
        using var db = _database.CreateContext();
        db.Add(new TicketEscalation(
            _ticketId, TicketPriority.High, TicketPriority.High, null, null, null, null,
            "Same priority", "alex@test.example", Now));

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void AnEscalation_ForATicketThatDoesNotExist_IsRejectedByTheDatabase()
    {
        using var db = _database.CreateContext();
        db.Add(new TicketEscalation(
            _ticketId + 999, TicketPriority.Medium, TicketPriority.High, null, null, null, null,
            "No such ticket", "alex@test.example", Now));

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }
}
