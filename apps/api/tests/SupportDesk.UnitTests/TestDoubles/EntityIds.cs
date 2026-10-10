using SupportDesk.Domain.Common;

namespace SupportDesk.UnitTests.TestDoubles;

internal static class EntityIds
{
    /// <summary>
    /// Gives an unsaved entity an id. In production the database assigns it, but a test that
    /// never touches a database still needs ids to tell its entities apart.
    /// </summary>
    public static T WithId<T>(this T entity, int id) where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

        return entity;
    }
}
