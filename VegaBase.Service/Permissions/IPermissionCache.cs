using VegaBase.Service.Infrastructure.Cache;

namespace VegaBase.Service.Permissions;

/// <summary>
/// Permission cache keyed by RoleId.
/// Adds LoadAsync() to warm per-role permissions and HasPermission() to check.
/// <para>
/// <b>Registration requirement (F3):</b> must be registered as a <b>Singleton</b>.
/// Registering as Scoped or Transient means each request gets an empty cache —
/// no permissions will be found and all non-admin actions will return 403.
/// </para>
/// </summary>
public interface IPermissionCache : ICacheStore<Guid, RolePermissionCache>
{
    /// <summary>
    /// Pre-warm cache for one role. Idempotent by default — if already cached, skip.
    /// Pass <paramref name="overwrite"/>=true to force replacement (equivalent to
    /// <see cref="ICacheStore{TKey, TCacheModel}.Invalidate"/> + Load).
    /// </summary>
    Task LoadAsync(Guid roleId, IEnumerable<PermissionEntry> permissions, bool overwrite = false);

    /// <summary>
    /// Check permission — O(1), no DB hit.
    /// action: "view" | "create" | "edit" | "delete" (case-insensitive)
    /// </summary>
    bool HasPermission(Guid roleId, string screenCode, string action);

    /// <summary>Union (OR) across multiple roles.</summary>
    bool HasPermission(IEnumerable<Guid> roleIds, string screenCode, string action);
}
