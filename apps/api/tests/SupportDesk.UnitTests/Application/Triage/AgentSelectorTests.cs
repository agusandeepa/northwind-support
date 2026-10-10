using SupportDesk.Application.Triage;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Triage;

public class AgentSelectorTests
{
    private static readonly Category General = new Category("General", false, false).WithId(1);
    private static readonly Category Billing = new Category("Billing", true, false).WithId(2);

    private readonly AgentSelector _selector = new();

    private static AgentWorkload Workload(
        int id, int openTickets, int maxOpen = 10, bool active = true, int[]? specializations = null)
    {
        var agent = new Agent($"Agent {id}", $"agent{id}@test.example", maxOpen, FixedClock.DefaultNow).WithId(id);

        foreach (var categoryId in specializations ?? [])
        {
            agent.AddSpecialization(categoryId);
        }

        if (!active)
        {
            agent.Deactivate();
        }

        return new AgentWorkload(agent, openTickets);
    }

    [Fact]
    public void Choose_PicksTheAgentWithTheFewestOpenTickets()
    {
        var result = _selector.Choose([Workload(1, 5), Workload(2, 2), Workload(3, 7)], General);

        Assert.Equal(2, result.Agent!.Id);
    }

    [Fact]
    public void Choose_BreaksTiesByTheLowestId()
    {
        var result = _selector.Choose([Workload(5, 3), Workload(3, 3), Workload(4, 3)], General);

        Assert.Equal(3, result.Agent!.Id);
    }

    [Fact]
    public void Choose_SkipsAgentsAtTheirLimit()
    {
        var result = _selector.Choose([Workload(1, 6, maxOpen: 6), Workload(2, 9)], General);

        Assert.Equal(2, result.Agent!.Id);
    }

    [Fact]
    public void Choose_SkipsInactiveAgents()
    {
        var result = _selector.Choose([Workload(1, 0, active: false), Workload(2, 8)], General);

        Assert.Equal(2, result.Agent!.Id);
    }

    [Fact]
    public void Choose_ForASpecialistCategory_OnlyConsidersSpecialists()
    {
        var result = _selector.Choose(
            [Workload(1, 0), Workload(2, 6, specializations: [Billing.Id])],
            Billing);

        Assert.Equal(2, result.Agent!.Id);
    }

    [Fact]
    public void Choose_ForANonSpecialistCategory_AnyEligibleAgentWillDo()
    {
        var result = _selector.Choose([Workload(1, 0), Workload(2, 6, specializations: [Billing.Id])], General);

        Assert.Equal(1, result.Agent!.Id);
    }

    [Fact]
    public void Choose_WhenEveryoneIsFull_LeavesTheTicketUnassignedWithAReason()
    {
        var result = _selector.Choose([Workload(1, 6, maxOpen: 6), Workload(2, 8, maxOpen: 8)], General);

        Assert.Null(result.Agent);
        Assert.Contains("limit", result.Reason);
    }

    [Fact]
    public void Choose_WhenNobodyIsSpecialised_NamesTheCategoryInTheReason()
    {
        var result = _selector.Choose([Workload(1, 0), Workload(2, 0)], Billing);

        Assert.Null(result.Agent);
        Assert.Contains("Billing", result.Reason);
    }

    [Fact]
    public void Choose_WithNoAgentsAtAll_LeavesTheTicketUnassigned()
    {
        var result = _selector.Choose([], General);

        Assert.Null(result.Agent);
    }

    [Fact]
    public void KeepOrChoose_KeepsTheCurrentAgentEvenWhenTheirOwnTicketFillsTheirLimit()
    {
        // 6 of 6 includes the very ticket being re-checked, so they still have room for it.
        var result = _selector.KeepOrChoose(1, [Workload(1, 6, maxOpen: 6), Workload(2, 0)], General);

        Assert.Equal(1, result.Agent!.Id);
    }

    [Fact]
    public void KeepOrChoose_KeepsTheCurrentAgentEvenWhenSomeoneElseHasFewerTickets()
    {
        var result = _selector.KeepOrChoose(1, [Workload(1, 5), Workload(2, 1)], General);

        Assert.Equal(1, result.Agent!.Id);
    }

    [Fact]
    public void KeepOrChoose_ReassignsWhenTheCurrentAgentIsInactive()
    {
        var result = _selector.KeepOrChoose(1, [Workload(1, 1, active: false), Workload(2, 4)], General);

        Assert.Equal(2, result.Agent!.Id);
    }

    [Fact]
    public void KeepOrChoose_ReassignsWhenTheCurrentAgentIsNotSpecialised()
    {
        var result = _selector.KeepOrChoose(
            1,
            [Workload(1, 1), Workload(2, 4, specializations: [Billing.Id])],
            Billing);

        Assert.Equal(2, result.Agent!.Id);
    }

    [Fact]
    public void KeepOrChoose_WithNoCurrentAgent_ChoosesLikeChoose()
    {
        var result = _selector.KeepOrChoose(null, [Workload(1, 5), Workload(2, 2)], General);

        Assert.Equal(2, result.Agent!.Id);
    }

    [Fact]
    public void KeepOrChoose_WhenNoOneIsEligible_LeavesTheTicketUnassigned()
    {
        var result = _selector.KeepOrChoose(1, [Workload(1, 1, active: false)], General);

        Assert.Null(result.Agent);
    }
}
