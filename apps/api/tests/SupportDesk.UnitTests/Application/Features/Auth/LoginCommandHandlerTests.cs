using Microsoft.Extensions.Logging.Abstractions;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.Login;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;
using SupportDesk.Infrastructure.Services;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Auth;

public class LoginCommandHandlerTests
{
    private const string Email = "alex@test.example";
    private const string Password = "correct horse";

    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 1000);
    private readonly Mock<IAgentRepository> _agents = new();
    private readonly Mock<ITokenIssuer> _tokens = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _tokens.Setup(t => t.Issue(It.IsAny<Agent>()))
            .Returns(new AccessToken("the-token", FixedClock.DefaultNow.AddHours(8)));

        _handler = new LoginCommandHandler(_agents.Object, _hasher, _tokens.Object, NullLogger<LoginCommandHandler>.Instance);
    }

    private Agent AnAgent(string? password = Password, bool active = true)
    {
        var agent = new Agent("Alex Turner", Email, 10, FixedClock.DefaultNow).WithId(3);

        if (password is not null)
        {
            agent.SetPasswordHash(_hasher.Hash(password));
        }

        if (!active)
        {
            agent.Deactivate();
        }

        _agents.Setup(a => a.GetByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        return agent;
    }

    private Task<LoginResponse> SignIn(string email = Email, string password = Password) =>
        _handler.HandleAsync(new LoginRequest(email, password), CancellationToken.None);

    [Fact]
    public async Task SigningInWithTheRightPassword_ReturnsATokenAndWhoSignedIn()
    {
        AnAgent();

        var response = await SignIn();

        Assert.Equal("the-token", response.Token);
        Assert.Equal(FixedClock.DefaultNow.AddHours(8), response.ExpiresAtUtc);
        Assert.Equal(new CurrentUserDto(3, "Alex Turner", Email), response.User);
    }

    [Fact]
    public async Task SigningInWithAWrongPassword_IsRefusedAndNoTokenIsIssued()
    {
        AnAgent();

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => SignIn(password: "wrong"));

        Assert.Equal(LoginCommandHandler.FailureMessage, exception.Message);
        _tokens.Verify(t => t.Issue(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task AnUnknownEmail_GetsExactlyTheSameRefusalAsAWrongPassword()
    {
        AnAgent();

        var wrongPassword = await Assert.ThrowsAsync<AuthenticationFailedException>(() => SignIn(password: "wrong"));
        var unknownEmail = await Assert.ThrowsAsync<AuthenticationFailedException>(() => SignIn(email: "nobody@test.example"));

        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
        _tokens.Verify(t => t.Issue(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task AnInactiveAgent_CannotSignInEvenWithTheRightPassword()
    {
        AnAgent(active: false);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => SignIn());

        Assert.Equal(LoginCommandHandler.FailureMessage, exception.Message);
        _tokens.Verify(t => t.Issue(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task AnAgentWithNoPasswordSet_CannotSignIn()
    {
        AnAgent(password: null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => SignIn(password: ""));
    }

    [Fact]
    public async Task TheEmailIsTrimmedBeforeTheLookup()
    {
        AnAgent();

        var response = await SignIn(email: "  alex@test.example  ");

        Assert.Equal("the-token", response.Token);
    }
}
