using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Application.Abstractions;

/// <summary>An access token and the moment it stops being valid.</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

/// <summary>Issues the token an agent presents on every request after signing in.</summary>
public interface ITokenIssuer
{
    AccessToken Issue(Agent agent);
}
