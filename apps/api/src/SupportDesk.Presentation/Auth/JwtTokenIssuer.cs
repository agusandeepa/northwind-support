using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Presentation.Auth;

/// <summary>Issues signed JWTs that say who the agent is and when the token stops being valid.</summary>
public sealed class JwtTokenIssuer(JwtOptions options, IClock clock) : ITokenIssuer
{
    public AccessToken Issue(Agent agent)
    {
        var now = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var expires = now + options.Lifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(AgentClaims.Name, agent.FullName),
                new Claim(JwtRegisteredClaimNames.Email, agent.Email)
            ]),
            SigningCredentials = new SigningCredentials(options.SigningKey, SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
