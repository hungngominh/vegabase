using VegaBase.Service.Infrastructure.Cache;

namespace VegaBase.Service.Permissions;

public class PermissionCache
    : MemoryCacheStore<Guid, RolePermissionCache>, IPermissionCache
{
    protected override Guid ExtractKey(RolePermissionCache item) => item.RoleId;

    public Task LoadAsync(Guid roleId, IEnumerable<PermissionEntry> permissions)
    {
        if (GetItem(roleId, _ => null) != null)
            return Task.CompletedTask;

        _store[roleId] = new RolePermissionCache
        {
            RoleId = roleId,
            Screens = permissions.ToDictionary(
                p => p.ScreenCode,
                p => new ScreenActions
                {
                    CanView   = p.CanView,
                    CanCreate = p.CanCreate,
                    CanEdit   = p.CanEdit,
                    CanDelete = p.CanDelete,
                })
        };

        return Task.CompletedTask;
    }

    public bool HasPermission(Guid roleId, string screenCode, string action)
    {
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
