using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.GetCurrentUser;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Auth;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<IAgentRepository> _agents = new();

    private GetCurrentUserQueryHandler Handler() => new(_agents.Object);

    [Fact]
    public async Task ReturnsTheAgentTheTokenBelongsTo()
    {
        var agent = new Agent("Alex Turner", "alex@test.example", 10, FixedClock.DefaultNow).WithId(3);
        _agents.Setup(a => a.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        var user = await Handler().HandleAsync(3, CancellationToken.None);

        Assert.Equal(new CurrentUserDto(3, "Alex Turner", "alex@test.example"), user);
    }

    [Fact]
    public async Task AnAgentWhoIsGone_IsNotSignedIn() =>
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Handler().HandleAsync(99, CancellationToken.None));

    [Fact]
    public async Task AnAgentWhoWasDeactivated_IsNotSignedIn()
    {
        var agent = new Agent("Yuki Tanaka", "yuki@test.example", 8, FixedClock.DefaultNow).WithId(6);
        agent.Deactivate();
        _agents.Setup(a => a.GetByIdAsync(6, It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Handler().HandleAsync(6, CancellationToken.None));
    }
}
