using VegaBase.Service.Infrastructure.Cache;

namespace VegaBase.Service.Permissions;

public class PermissionCache
    : MemoryCacheStore<Guid, RolePermissionCache>, IPermissionCache
{
    protected override Guid ExtractKey(RolePermissionCache item) => item.RoleId;

    public Task LoadAsync(Guid roleId, IEnumerable<PermissionEntry> permissions, bool overwrite = false)
    {
        if (!overwrite && _store.ContainsKey(roleId))
            return Task.CompletedTask;

        var screens = permissions
            .Where(p => !string.IsNullOrEmpty(p.ScreenCode))
            .GroupBy(p => p.ScreenCode)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    return new ScreenActions
                    {
                        CanView   = g.Any(p => p.CanView),
                        CanCreate = g.Any(p => p.CanCreate),
                        CanEdit   = g.Any(p => p.CanEdit),
                        CanDelete = g.Any(p => p.CanDelete),
                    };
                });

        var snapshot = new RolePermissionCache { RoleId = roleId, Screens = screens };
        _store.AddOrUpdate(roleId, snapshot, (_, _) => snapshot);

        return Task.CompletedTask;
    }

    public bool HasPermission(Guid roleId, string screenCode, string action)
    {
        if (string.IsNullOrEmpty(screenCode) || string.IsNullOrEmpty(action)) return false;

        if (!_store.TryGetValue(roleId, out var role) || role is null) return false;
        if (!role.Screens.TryGetValue(screenCode, out var screen)) return false;

        if (string.Equals(action, "view",   StringComparison.OrdinalIgnoreCase)) return screen.CanView;
        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)) return screen.CanCreate;
        if (string.Equals(action, "edit",   StringComparison.OrdinalIgnoreCase)) return screen.CanEdit;
        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase)) return screen.CanDelete;
        return false;
    }

    public bool HasPermission(IEnumerable<Guid> roleIds, string screenCode, string action)
    {
        foreach (var roleId in roleIds)
            if (HasPermission(roleId, screenCode, action))
                return true;
        return false;
    }
}
