using System.Linq.Expressions;

namespace SupportDesk.Infrastructure.Queries;

internal static class QueryableExtensions
{
    /// <summary>
    /// Applies <paramref name="predicate"/> only when <paramref name="condition"/> holds, so an
    /// optional filter is one line at the call site and is left out of the SQL when not supplied.
    /// </summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, bool>> predicate) =>
        condition ? source.Where(predicate) : source;
}
