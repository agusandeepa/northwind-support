using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaEvaluatorTests
{
    private const int AtRiskPercent = 25;

    private static readonly DateTime Now = FixedClock.DefaultNow;

    // A 4 hour window that started 3 hours ago, so exactly 1 hour (25%) is left at Now.
    private static readonly DateTime Start = Now.AddHours(-3);

    private static SlaStatus Open(DateTime due, int percent = AtRiskPercent) =>
        SlaEvaluator.Evaluate(Start, due, null, Now, percent);

    [Fact]
    public void Evaluate_WithoutADueDate_IsNotApplicable() =>
        Assert.Equal(SlaStatus.NotApplicable, SlaEvaluator.Evaluate(Start, null, null, Now, AtRiskPercent));

    [Fact]
    public void Evaluate_ResolvedExactlyAtTheDueDate_IsMet() =>
        Assert.Equal(SlaStatus.Met, SlaEvaluator.Evaluate(Start, Now, Now, Now.AddDays(1), AtRiskPercent));

    [Fact]
    public void Evaluate_ResolvedBeforeTheDueDate_IsMet() =>
        Assert.Equal(SlaStatus.Met, SlaEvaluator.Evaluate(Start, Now, Now.AddMinutes(-1), Now.AddDays(1), AtRiskPercent));

    [Fact]
    public void Evaluate_ResolvedOneTickAfterTheDueDate_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Start, Now, Now.AddTicks(1), Now, AtRiskPercent));

    [Fact]
    public void Evaluate_ResolvedNearTheDueDate_IsMetNotAtRisk() =>
        Assert.Equal(SlaStatus.Met, SlaEvaluator.Evaluate(Start, Now.AddMinutes(1), Now, Now, AtRiskPercent));

    [Fact]
    public void Evaluate_OpenOneTickPastTheDueDate_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, Open(Now.AddTicks(-1)));

    [Fact]
    public void Evaluate_OpenExactlyAtTheDueDate_IsAtRiskNotBreached() =>
        Assert.Equal(SlaStatus.AtRisk, Open(Now));

    [Fact]
    public void Evaluate_OpenWithExactlyTheThresholdLeft_IsAtRisk() =>
        Assert.Equal(SlaStatus.AtRisk, Open(Now.AddHours(1)));

    [Fact]
    public void Evaluate_OpenWithOneTickMoreThanTheThresholdLeft_IsWithinSla() =>
        Assert.Equal(SlaStatus.WithinSla, Open(Now.AddHours(1).AddTicks(1)));

    [Fact]
    public void Evaluate_OpenWithHalfTheWindowLeft_IsWithinSla() =>
        Assert.Equal(SlaStatus.WithinSla, Open(Now.AddHours(2)));

    [Fact]
    public void Evaluate_FollowsTheConfiguredThreshold()
    {
        // 1 hour of a 4 hour window is left: 25%.
        Assert.Equal(SlaStatus.AtRisk, Open(Now.AddHours(1), percent: 50));
        Assert.Equal(SlaStatus.WithinSla, Open(Now.AddHours(1), percent: 20));
    }

    [Fact]
    public void Evaluate_WithAZeroLengthWindow_IsAtRiskOnlyAtTheDueDate()
    {
        Assert.Equal(SlaStatus.AtRisk, SlaEvaluator.Evaluate(Now, Now, null, Now, AtRiskPercent));
        Assert.Equal(SlaStatus.WithinSla, SlaEvaluator.Evaluate(Now, Now.AddMinutes(1), null, Now, AtRiskPercent));
    }
}
