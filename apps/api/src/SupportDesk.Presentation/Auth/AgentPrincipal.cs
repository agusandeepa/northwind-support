using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using SupportDesk.Application.Exceptions;

namespace SupportDesk.Presentation.Auth;

public static class AgentClaims
{
    public const string Name = "name";
}

/// <summary>Reads who is signed in from the validated token, so a request body never has to say.</summary>
public static class AgentPrincipal
{
    public static int AgentId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : throw new AuthenticationFailedException("The token does not say who is signed in.");

    public static string AgentName(this ClaimsPrincipal user) =>
        user.FindFirst(AgentClaims.Name)?.Value is { Length: > 0 } name
            ? name
            : throw new AuthenticationFailedException("The token does not say who is signed in.");
}
