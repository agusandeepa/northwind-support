using SupportDesk.Application.Contracts.Common;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

/// <summary>
/// The ticket list's filters, run as real SQL against an in-memory database. Each test checks
/// the rows that come back and the count and paging that describe them.
/// </summary>
public sealed class TicketQueriesFilterTests : IDisposable
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    private readonly SqliteTestDatabase _database = new();

    private readonly Customer _contoso = new("Contoso", "help@contoso.test", null, CustomerTier.Premium, Now);
    private readonly Customer _fabrikam = new("Fabrikam", "help@fabrikam.test", null, CustomerTier.Standard, Now);
    private readonly Category _billing = new("Billing", requiresSpecialist: true, forcesCriticalPriority: false);
    private readonly Category _technical = new("Technical", requiresSpecialist: false, forcesCriticalPriority: false);
    private readonly Agent _alex = new("Alex Turner", "alex@northwind.test", 10, Now);
    private readonly Agent _ben = new("Ben Osei", "ben@northwind.test", 8, Now);

    // Five tickets, created in this order, so their ids are 1 to 5.
    //   TCK-0001  Invoice is wrong        Contoso   Billing    High      Open        Alex
    //   TCK-0002  Cannot log in           Fabrikam  Technical  Critical  New         -
    //   TCK-0003  Slow dashboard          Contoso   Technical  High      InProgress  Ben
    //   TCK-0004  Refund request          Fabrikam  Billing    Low       Open        -
    //   TCK-0005  Password reset email    Contoso   Technical  Medium    Resolved    Alex
    public TicketQueriesFilterTests()
    {
        using var db = _database.CreateContext();

        db.AddRange(_contoso, _fabrikam, _billing, _technical, _alex, _ben);
        db.SaveChanges();

        Add(db, "TCK-0001", "Invoice is wrong", _contoso, _billing, TicketPriority.High, TicketStatus.Open, _alex);
        Add(db, "TCK-0002", "Cannot log in", _fabrikam, _technical, TicketPriority.Critical, TicketStatus.New, null);
        Add(db, "TCK-0003", "Slow dashboard", _contoso, _technical, TicketPriority.High, TicketStatus.InProgress, _ben);
        Add(db, "TCK-0004", "Refund request", _fabrikam, _billing, TicketPriority.Low, TicketStatus.Open, null);
        Add(db, "TCK-0005", "Password reset email", _contoso, _technical, TicketPriority.Medium, TicketStatus.Resolved, _alex);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task NoFilters_ReturnsEveryTicket()
    {
        var result = await Run(new TicketQuery());

        Assert.Equal(new[] { "TCK-0001", "TCK-0002", "TCK-0003", "TCK-0004", "TCK-0005" }, References(result));
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task Priority_IsAnExactMatch_NotAMinimum()
    {
        var result = await Run(new TicketQuery { Priority = TicketPriority.High });

        // The Critical ticket is more urgent than High, but it is not High.
        Assert.Equal(new[] { "TCK-0001", "TCK-0003" }, References(result));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Status_IsAnExactMatch()
    {
        var result = await Run(new TicketQuery { Status = TicketStatus.Open });

        Assert.Equal(new[] { "TCK-0001", "TCK-0004" }, References(result));
    }

    [Fact]
    public async Task CategoryId_IsAnExactMatch()
    {
        var result = await Run(new TicketQuery { CategoryId = _billing.Id });

        Assert.Equal(new[] { "TCK-0001", "TCK-0004" }, References(result));
    }

    [Fact]
    public async Task CustomerId_IsAnExactMatch()
    {
        var result = await Run(new TicketQuery { CustomerId = _fabrikam.Id });

        Assert.Equal(new[] { "TCK-0002", "TCK-0004" }, References(result));
    }

    [Fact]
    public async Task AssignedAgentId_IsAnExactMatch()
    {
        var result = await Run(new TicketQuery { AssignedAgentId = _alex.Id });

        Assert.Equal(new[] { "TCK-0001", "TCK-0005" }, References(result));
    }

    [Fact]
    public async Task UnassignedOnly_ReturnsOnlyTicketsWithNoAgent()
    {
        var result = await Run(new TicketQuery { UnassignedOnly = true });

        Assert.Equal(new[] { "TCK-0002", "TCK-0004" }, References(result));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task UnassignedOnly_WhenFalse_DoesNotFilter()
    {
        var result = await Run(new TicketQuery { UnassignedOnly = false });

        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task Filters_AreCombinedWithAnd()
    {
        // Open AND Contoso: the other Open ticket is Fabrikam's, the other Contoso ones are not Open.
        var result = await Run(new TicketQuery { Status = TicketStatus.Open, CustomerId = _contoso.Id });

        Assert.Equal(new[] { "TCK-0001" }, References(result));
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Filters_ThatNoTicketSatisfiesTogether_ReturnNothing()
    {
        // Ben only holds a High ticket; none of his tickets is Low.
        var result = await Run(new TicketQuery { AssignedAgentId = _ben.Id, Priority = TicketPriority.Low });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task UnassignedOnly_WithAnAgentFilter_IsAnAndSoNothingMatches()
    {
        var result = await Run(new TicketQuery { UnassignedOnly = true, AssignedAgentId = _alex.Id });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Search_MatchesTheCustomerName()
    {
        var result = await Run(new TicketQuery { Search = "Contoso" });

        Assert.Equal(new[] { "TCK-0001", "TCK-0003", "TCK-0005" }, References(result));
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task Search_MatchesPartOfTheTitle()
    {
        var result = await Run(new TicketQuery { Search = "dash" });

        Assert.Equal(new[] { "TCK-0003" }, References(result));
    }

    [Fact]
    public async Task Search_MatchesTheReference()
    {
        var result = await Run(new TicketQuery { Search = "TCK-0004" });

        Assert.Equal(new[] { "TCK-0004" }, References(result));
    }

    [Fact]
    public async Task Search_IsTrimmed()
    {
        var result = await Run(new TicketQuery { Search = "  Contoso  " });

        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task Search_WhenBlank_IsIgnored()
    {
        var result = await Run(new TicketQuery { Search = "   " });

        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task Search_CombinesWithTheOtherFilters()
    {
        var result = await Run(new TicketQuery { Search = "Contoso", Status = TicketStatus.Resolved });

        Assert.Equal(new[] { "TCK-0005" }, References(result));
    }

    [Fact]
    public async Task TotalCountAndPaging_DescribeTheFilteredSet_NotAllTickets()
    {
        // Contoso has three tickets. Newest first (ties broken by id), page 1 is 5 then 3.
        var firstPage = await Run(new TicketQuery { Search = "Contoso", Page = 1, PageSize = 2 });
        var secondPage = await Run(new TicketQuery { Search = "Contoso", Page = 2, PageSize = 2 });

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(new[] { "TCK-0005", "TCK-0003" }, firstPage.Items.Select(i => i.Reference).ToArray());

        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(new[] { "TCK-0001" }, secondPage.Items.Select(i => i.Reference).ToArray());
    }

    private async Task<PagedResult<TicketListItemDto>> Run(TicketQuery query)
    {
        using var db = _database.CreateContext();

        var queries = new TicketQueries(db, new FixedClock());

        return await queries.GetPagedAsync(query.Normalized(), CancellationToken.None);
    }

    private static string[] References(PagedResult<TicketListItemDto> result) =>
        result.Items.Select(i => i.Reference).Order().ToArray();

    private static void Add(
        SupportDbContext db,
        string reference,
        string title,
        Customer customer,
        Category category,
        TicketPriority priority,
        TicketStatus status,
        Agent? agent)
    {
        var ticket = Ticket.Raise(reference, title, $"Details of: {title}", customer.Id, category.Id, priority, Now);

        if (agent is not null)
        {
            ticket.AssignTo(agent.Id, Now);
        }

        if (status != TicketStatus.New)
        {
            ticket.ChangeStatus(status, Now);
        }

        db.Add(ticket);
        db.SaveChanges();
    }
}
