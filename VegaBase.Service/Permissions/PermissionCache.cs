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
                    var first = g.First();
                    return new ScreenActions
                    {
                        CanView   = first.CanView,
                        CanCreate = first.CanCreate,
                        CanEdit   = first.CanEdit,
                        CanDelete = first.CanDelete,
                    };
                });

        var snapshot = new RolePermissionCache { RoleId = roleId, Screens = screens };
        _store.AddOrUpdate(roleId, snapshot, (_, _) => snapshot);

        return Task.CompletedTask;
    }

    public bool HasPermission(Guid roleId, string screenCode, string action)
    {
        if (string.IsNullOrEmpty(screenCode) || string.IsNullOrEmpty(action)) return false;

        var role = GetItem(roleId, _ => null);
        if (role is null) return false;
        if (!role.Screens.TryGetValue(screenCode, out var screen)) return false;

        return action.ToLowerInvariant() switch
        {
            "view"   => screen.CanView,
            "create" => screen.CanCreate,
            "edit"   => screen.CanEdit,
            "delete" => screen.CanDelete,
            _        => false
        };
    }

    public bool HasPermission(IEnumerable<Guid> roleIds, string screenCode, string action)
    {
        foreach (var roleId in roleIds)
            if (HasPermission(roleId, screenCode, action))
                return true;
        return false;
    }
}
