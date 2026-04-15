namespace VegaBase.Service.Permissions;

/// <summary>
/// DTO representing one screen's permission for a role.
/// Callers map their app-specific entity to this before calling IPermissionCache.LoadAsync.
/// </summary>
public record PermissionEntry(
    Guid   RoleId,
    string ScreenCode,
    bool   CanView,
    bool   CanCreate,
    bool   CanEdit,
    bool   CanDelete
);
