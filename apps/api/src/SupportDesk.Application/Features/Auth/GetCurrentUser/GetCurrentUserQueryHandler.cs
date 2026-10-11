using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.GetCurrentUser;

/// <summary>Who the token belongs to, checked against the database so a deactivated agent is out at once.</summary>
public sealed class GetCurrentUserQueryHandler(IAgentRepository agents)
{
    /// <exception cref="AuthenticationFailedException">The agent no longer exists or is inactive.</exception>
    public async Task<CurrentUserDto> HandleAsync(int agentId, CancellationToken ct)
    {
        var agent = await agents.GetByIdAsync(agentId, ct);

        if (agent is null || !agent.IsActive)
        {
            throw new AuthenticationFailedException("The signed-in user is no longer valid.");
        }

        return new CurrentUserDto(agent.Id, agent.FullName, agent.Email);
    }
}
