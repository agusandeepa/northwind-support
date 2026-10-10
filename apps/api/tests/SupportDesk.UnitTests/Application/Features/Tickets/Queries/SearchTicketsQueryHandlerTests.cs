using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Common;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Features.Tickets.Queries.SearchTickets;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Queries;

public class SearchTicketsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_PassesTheNormalizedQueryToTheDatabase()
    {
        TicketQuery? received = null;
        var tickets = new Mock<ITicketQueries>();
        tickets
            .Setup(t => t.GetPagedAsync(It.IsAny<TicketQuery>(), It.IsAny<CancellationToken>()))
            .Callback<TicketQuery, CancellationToken>((query, _) => received = query)
            .ReturnsAsync(new PagedResult<TicketListItemDto>([], 1, 20, 0));

        var handler = new SearchTicketsQueryHandler(tickets.Object);

        await handler.HandleAsync(new TicketQuery { Search = "  invoice  ", Page = 0 }, CancellationToken.None);

        Assert.NotNull(received);
        Assert.Equal("invoice", received.Search);
        Assert.Equal(1, received.Page);
    }
}
