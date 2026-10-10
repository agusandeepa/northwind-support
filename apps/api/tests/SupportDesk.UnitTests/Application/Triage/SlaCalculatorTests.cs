using SupportDesk.Application.Triage;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Triage;

public class SlaCalculatorTests
{
    private static SlaOptions Options(double multiplier = 0.5, double minimumHours = 1) =>
        TestSlaOptions.Create(multiplier, minimumHours);

    [Theory]
    [InlineData(TicketPriority.Low, 72.0)]
    [InlineData(TicketPriority.Medium, 24.0)]
    [InlineData(TicketPriority.High, 8.0)]
    [InlineData(TicketPriority.Critical, 4.0)]
    public void WindowFor_StandardCustomer_IsTheBaseWindow(TicketPriority priority, double hours) =>
        Assert.Equal(TimeSpan.FromHours(hours), new SlaCalculator(Options()).WindowFor(priority, CustomerTier.Standard).Duration);

    [Theory]
    [InlineData(TicketPriority.Low, 36.0)]
    [InlineData(TicketPriority.Medium, 12.0)]
    [InlineData(TicketPriority.High, 4.0)]
    [InlineData(TicketPriority.Critical, 2.0)]
    public void WindowFor_PremiumCustomer_IsHalfTheBaseWindow(TicketPriority priority, double hours) =>
        Assert.Equal(TimeSpan.FromHours(hours), new SlaCalculator(Options()).WindowFor(priority, CustomerTier.Premium).Duration);

    [Fact]
    public void WindowFor_PremiumCustomer_IsNeverShorterThanTheFloor()
    {
        // 4 h x 0.1 = 0.4 h, which is below the 1 h floor.
        var window = new SlaCalculator(Options(multiplier: 0.1)).WindowFor(TicketPriority.Critical, CustomerTier.Premium);

        Assert.Equal(TimeSpan.FromHours(1), window.Duration);
        Assert.Contains("minimum", window.Reason);
    }

    [Fact]
    public void WindowFor_PremiumCustomer_ExactlyAtTheFloor_IsNotRaised()
    {
        // 4 h x 0.25 = exactly 1 h.
        var window = new SlaCalculator(Options(multiplier: 0.25)).WindowFor(TicketPriority.Critical, CustomerTier.Premium);

        Assert.Equal(TimeSpan.FromHours(1), window.Duration);
        Assert.DoesNotContain("minimum", window.Reason);
    }

    [Fact]
    public void WindowFor_FollowsTheConfiguration()
    {
        var options = Options();
        options.BaseWindowHours[TicketPriority.Medium] = 10;

        Assert.Equal(
            TimeSpan.FromHours(10),
            new SlaCalculator(options).WindowFor(TicketPriority.Medium, CustomerTier.Standard).Duration);
    }

    [Fact]
    public void WindowFor_WithNoConfiguredWindow_Throws()
    {
        var options = Options();
        options.BaseWindowHours.Remove(TicketPriority.High);

        Assert.Throws<InvalidOperationException>(
            () => new SlaCalculator(options).WindowFor(TicketPriority.High, CustomerTier.Standard));
    }

    [Fact]
    public void DueAt_IsTheStartPlusTheWindow()
    {
        var window = new SlaCalculator(Options()).WindowFor(TicketPriority.Medium, CustomerTier.Standard);

        Assert.Equal(FixedClock.DefaultNow.AddHours(24), window.DueAt(FixedClock.DefaultNow));
    }

    [Fact]
    public void WindowFor_ExplainsTheWindow()
    {
        var calculator = new SlaCalculator(Options());

        Assert.Equal(
            "Medium priority: 24 h base window.",
            calculator.WindowFor(TicketPriority.Medium, CustomerTier.Standard).Reason);

        Assert.Equal(
            "High priority: 8 h base window x 0.5 for a Premium customer = 4 h.",
            calculator.WindowFor(TicketPriority.High, CustomerTier.Premium).Reason);
    }
}
