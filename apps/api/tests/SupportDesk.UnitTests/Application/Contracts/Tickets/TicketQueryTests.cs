using SupportDesk.Application.Contracts.Tickets;

namespace SupportDesk.UnitTests.Application.Contracts.Tickets;

public class TicketQueryTests
{
    [Theory]
    [InlineData("  invoice  ", "invoice")]
    [InlineData("\tTCK-0042\n", "TCK-0042")]
    [InlineData("two words", "two words")]
    public void Normalized_TrimsTheSearchText(string search, string expected)
    {
        var query = new TicketQuery { Search = search }.Normalized();

        Assert.Equal(expected, query.Search);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalized_TreatsABlankSearchAsNoSearch(string? search)
    {
        var query = new TicketQuery { Search = search }.Normalized();

        Assert.Null(query.Search);
    }

    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-3, -1, 1, 20)]
    [InlineData(2, 500, 2, TicketQuery.MaxPageSize)]
    public void Normalized_ClampsPagingToWhatTheDatabaseCanServe(
        int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var query = new TicketQuery { Page = page, PageSize = pageSize }.Normalized();

        Assert.Equal(expectedPage, query.Page);
        Assert.Equal(expectedPageSize, query.PageSize);
    }
}
