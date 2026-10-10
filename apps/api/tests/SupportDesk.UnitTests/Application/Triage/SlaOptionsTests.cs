using SupportDesk.Application.Triage;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Triage;

public class SlaOptionsTests
{
    [Fact]
    public void EnsureValid_AcceptsTheConfiguredRules() =>
        Assert.NotNull(TestSlaOptions.Create().EnsureValid());

    [Fact]
    public void EnsureValid_RejectsAPriorityWithNoWindow()
    {
        var options = TestSlaOptions.Create();
        options.BaseWindowHours.Remove(TicketPriority.High);

        Assert.Throws<InvalidOperationException>(options.EnsureValid);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.5)]
    public void EnsureValid_RejectsAMultiplierOutsideZeroToOne(double multiplier) =>
        Assert.Throws<InvalidOperationException>(TestSlaOptions.Create(multiplier: multiplier).EnsureValid);

    [Fact]
    public void EnsureValid_RejectsAThresholdAbove100Percent()
    {
        var options = TestSlaOptions.Create();
        options.AtRiskThresholdPercent = 101;

        Assert.Throws<InvalidOperationException>(options.EnsureValid);
    }
}
