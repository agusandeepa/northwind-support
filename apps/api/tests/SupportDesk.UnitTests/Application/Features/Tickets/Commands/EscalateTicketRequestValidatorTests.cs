using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public class EscalateTicketRequestValidatorTests
{
    private readonly EscalateTicketRequestValidator _validator = new();

    private bool IsValid(string reason, string escalatedBy = "alex@test.example") =>
        _validator.Validate(new EscalateTicketRequest(reason, escalatedBy)).IsValid;

    [Theory]
    [InlineData("abcde")]
    [InlineData("  abcde  ")]
    public void AReasonOfFiveCharacters_IsAccepted(string reason) => Assert.True(IsValid(reason));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcd")]
    [InlineData("   abcd   ")]
    public void AShortOrBlankReason_IsRejected(string reason) => Assert.False(IsValid(reason));

    [Fact]
    public void AReasonOf500Characters_IsAccepted() => Assert.True(IsValid(new string('a', 500)));

    [Fact]
    public void AReasonOf501Characters_IsRejected() => Assert.False(IsValid(new string('a', 501)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankActor_IsRejected(string actor) => Assert.False(IsValid("A good reason", actor));

    [Fact]
    public void AnActorOver100Characters_IsRejected() => Assert.False(IsValid("A good reason", new string('a', 101)));
}
