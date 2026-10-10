using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Agents;

public class AgentTests
{
    private static readonly Category General = new Category("General", false, false).WithId(1);
    private static readonly Category Billing = new Category("Billing", true, false).WithId(2);

    private static Agent AnAgent(int maxOpenTickets = 5) =>
        new("Alex Turner", "alex@test.example", maxOpenTickets, FixedClock.DefaultNow);

    [Fact]
    public void CanTake_BelowTheLimit_IsTrue() =>
        Assert.True(AnAgent(maxOpenTickets: 5).CanTake(General, openTickets: 4));

    [Fact]
    public void CanTake_ExactlyAtTheLimit_IsFalse() =>
        Assert.False(AnAgent(maxOpenTickets: 5).CanTake(General, openTickets: 5));

    [Fact]
    public void CanTake_WhenInactive_IsFalse()
    {
        var agent = AnAgent();
        agent.Deactivate();

        Assert.False(agent.CanTake(General, openTickets: 0));
    }

    [Fact]
    public void CanTake_SpecialistCategory_NeedsTheSpecialization()
    {
        var specialist = AnAgent();
        specialist.AddSpecialization(Billing.Id);

        Assert.True(specialist.CanTake(Billing, openTickets: 0));
        Assert.False(AnAgent().CanTake(Billing, openTickets: 0));
    }

    [Fact]
    public void CanTake_NonSpecialistCategory_DoesNotNeedASpecialization() =>
        Assert.True(AnAgent().CanTake(General, openTickets: 0));

    [Fact]
    public void CanTake_SpecialistInAnotherCategory_IsNotQualifiedForThisOne()
    {
        var agent = AnAgent();
        agent.AddSpecialization(General.Id);

        Assert.False(agent.CanTake(Billing, openTickets: 0));
    }
}
