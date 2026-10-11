using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Presentation.Auth;
using SupportDesk.UnitTests.TestDoubles;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.UnitTests.Presentation;

public class JwtTokenIssuerTests
{
    private static SymmetricSecurityKey Key(char fill) => new(Encoding.UTF8.GetBytes(new string(fill, 32)));

    private static readonly JwtOptions Options = new()
    {
        Issuer = "Test",
        Audience = "TestWeb",
        Lifetime = TimeSpan.FromHours(8),
        SigningKey = Key('k')
    };

    private readonly FixedClock _clock = new();

    private AccessToken IssueFor(Agent agent) => new JwtTokenIssuer(Options, _clock).Issue(agent);

    private static Agent AnAgent() => new Agent("Alex Turner", "alex@test.example", 10, FixedClock.DefaultNow).WithId(3);

    [Fact]
    public void TheTokenSaysWhoTheAgentIs()
    {
        var token = new JsonWebToken(IssueFor(AnAgent()).Token);

        Assert.Equal("3", token.GetClaim("sub").Value);
        Assert.Equal("Alex Turner", token.GetClaim("name").Value);
        Assert.Equal("alex@test.example", token.GetClaim("email").Value);
        Assert.Equal("Test", token.Issuer);
    }

    [Fact]
    public void TheTokenStopsBeingValidAfterTheConfiguredLifetime()
    {
        var token = IssueFor(AnAgent());

        Assert.Equal(_clock.UtcNow.AddHours(8), token.ExpiresAtUtc);
        Assert.Equal(token.ExpiresAtUtc, new JsonWebToken(token.Token).ValidTo);
    }

    private static async Task<bool> IsValid(string token, SecurityKey key)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "Test",
            ValidAudience = "TestWeb",
            IssuerSigningKey = key,
            ValidateLifetime = false
        });

        return result.IsValid;
    }

    [Fact]
    public async Task TheTokenIsAcceptedWithTheSigningKey() =>
        Assert.True(await IsValid(IssueFor(AnAgent()).Token, Key('k')));

    [Fact]
    public async Task TheTokenIsRejectedWithAnyOtherKey() =>
        Assert.False(await IsValid(IssueFor(AnAgent()).Token, Key('x')));
}
