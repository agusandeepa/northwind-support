using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Login;

/// <summary>Signs an agent in with their email and password.</summary>
public sealed class LoginCommandHandler(
    IAgentRepository agents,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    ILogger<LoginCommandHandler> logger)
{
    /// <summary>One message for every way a sign-in can fail, so it never says which part was wrong.</summary>
    public const string FailureMessage = "Invalid email or password.";

    /// <exception cref="AuthenticationFailedException">
    /// The email is unknown, the password is wrong, or the agent is inactive.
    /// </exception>
    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var agent = await agents.GetByEmailAsync(request.Email.Trim(), ct);

        // Checked even when there is no such agent, so the time taken does not give them away.
        var passwordMatches = passwordHasher.Verify(request.Password, agent?.PasswordHash);

        if (agent is null || !passwordMatches || !agent.IsActive)
        {
            logger.LogInformation("A sign-in attempt was refused.");

            throw new AuthenticationFailedException(FailureMessage);
        }

        var token = tokenIssuer.Issue(agent);

        return new LoginResponse(
            token.Token,
            token.ExpiresAtUtc,
            new CurrentUserDto(agent.Id, agent.FullName, agent.Email));
    }
}
