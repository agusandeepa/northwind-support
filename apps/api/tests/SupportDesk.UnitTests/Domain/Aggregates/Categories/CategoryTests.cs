using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Domain.Aggregates.Categories;

public class CategoryTests
{
    private static readonly Category Plain = new("General", requiresSpecialist: false, forcesCriticalPriority: false);
    private static readonly Category Forcing = new("Outage", requiresSpecialist: true, forcesCriticalPriority: true);

    [Theory]
    [InlineData(TicketPriority.Low)]
    [InlineData(TicketPriority.Medium)]
    [InlineData(TicketPriority.High)]
    [InlineData(TicketPriority.Critical)]
    public void ResolvePriority_UsesTheRequestedPriority(TicketPriority requested) =>
        Assert.Equal(requested, Plain.ResolvePriority(requested));

    [Fact]
    public void ResolvePriority_WithNoRequest_DefaultsToMedium() =>
        Assert.Equal(TicketPriority.Medium, Plain.ResolvePriority(null));

    [Theory]
    [InlineData(TicketPriority.Low)]
    [InlineData(TicketPriority.Medium)]
    [InlineData(TicketPriority.High)]
    public void ResolvePriority_WhenTheCategoryForcesCritical_IsAlwaysCritical(TicketPriority requested) =>
        Assert.Equal(TicketPriority.Critical, Forcing.ResolvePriority(requested));

    [Fact]
    public void ResolvePriority_WhenTheCategoryForcesCritical_IsCriticalEvenWithNoRequest() =>
        Assert.Equal(TicketPriority.Critical, Forcing.ResolvePriority(null));
}
