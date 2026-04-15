namespace VegaBase.Service.Infrastructure.Cache;

/// <summary>
/// Snapshot permissions of one role — stored in cache.
/// </summary>
public class RolePermissionCache
{
    public Guid RoleId { get; init; }

    /// <summary>Key: screenCode — Value: allowed actions</summary>
    public Dictionary<string, ScreenActions> Screens { get; init; } = new();
}

public class ScreenActions
{
    public bool CanView   { get; init; }
    public bool CanCreate { get; init; }
    public bool CanEdit   { get; init; }
    public bool CanDelete { get; init; }
}
