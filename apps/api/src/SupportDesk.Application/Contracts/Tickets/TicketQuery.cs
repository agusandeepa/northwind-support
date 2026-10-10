using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// Filters, sorting and paging for the ticket list. Bound directly from the query string.
/// </summary>
/// <remarks>
/// Every filter supplied is applied, and they combine with AND. Filtering happens in the
/// database, before counting and paging.
/// </remarks>
public sealed record TicketQuery
{
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Matches the ticket title, its reference, or the customer name. Blank means no search;
    /// <see cref="Normalized"/> trims it.
    /// </summary>
    public string? Search { get; init; }

    public TicketStatus? Status { get; init; }

    public TicketPriority? Priority { get; init; }

    public int? CategoryId { get; init; }

    public int? CustomerId { get; init; }

    public int? AssignedAgentId { get; init; }

    /// <summary>When true, only tickets without an assigned agent.</summary>
    public bool UnassignedOnly { get; init; }

    /// <summary>Only tickets in this SLA state, as of now.</summary>
    public SlaStatus? SlaStatus { get; init; }

    /// <summary>createdAtUtc | updatedAtUtc | dueAtUtc | priority | status.</summary>
    public string SortBy { get; init; } = "createdAtUtc";

    /// <summary>asc | desc.</summary>
    public string SortDirection { get; init; } = "desc";

    /// <summary>
    /// Page and page size clamped to something a database can serve, and the search text
    /// trimmed (a blank search becomes null, meaning "no search").
    /// </summary>
    public TicketQuery Normalized() => this with
    {
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize)
    };
}
