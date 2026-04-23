# Caching

Quy tắc sử dụng in-memory cache — khi nào cache, khi nào invalidate, và giới hạn của cache.

---

## CA-01 — Chỉ cache dữ liệu ít thay đổi

Cache phù hợp cho: lookup tables, permission lists, category lists, config values.  
Cache không phù hợp cho: user data, search results, transactional data.

```csharp
// ✅ Đúng: cache danh sách role (thay đổi hiếm)
var roles = await _roleCache.GetAll(loader: () => _db.QueryAsync<Role>(...));

// ❌ Sai: cache kết quả tìm kiếm động
var searchResults = await _productCache.GetItem(param.Keyword, loader: ...);
```

---

## CA-02 — Dùng `ICacheStore<TKey, TCacheModel>`, không tự implement cache

```csharp
// ✅ Đúng: inject ICacheStore
public class RoleService : BaseService<Role, RoleModel, RoleParam>
{
    private readonly ICacheStore<Guid, RoleModel> _cache;
    public RoleService(ICacheStore<Guid, RoleModel> cache, ...) { _cache = cache; }
}

// ❌ Sai: tự implement cache với Dictionary
public class RoleService
{
    private static readonly Dictionary<Guid, RoleModel> _cache = new();
    private static readonly object _lock = new();
}
```

---

## CA-03 — Invalidate cache bằng cách override `OnChanged()`, không làm ở nơi khác

```csharp
// ✅ Đúng: invalidate trong OnChanged
protected override async Task OnChanged(Role entity, RoleParam param)
{
    await _cache.Invalidate(entity.Id);
}

// ❌ Sai: invalidate bên trong ApplyUpdate hoặc CheckAddCondition
protected override void ApplyUpdate(Role entity, RoleParam param)
{
    entity.Name = param.Name;
    _cache.Invalidate(entity.Id).GetAwaiter().GetResult(); // sai chỗ + blocking async
}
```

---

## CA-04 — Không cache sensitive data (password hash, token, PII)

```csharp
// ✅ Đúng: cache model không có sensitive fields
public class RoleModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

// ❌ Sai: cache model có sensitive data
var cached = await _userCache.GetItem(userId, () => new UserCacheModel
{
    Id = user.Id,
    PasswordHash = user.PasswordHash, // không cache
    RefreshToken = user.RefreshToken  // không cache
});
```

---

## CA-05 — Chỉ invalidate `PermissionCache` khi role hoặc permission thực sự thay đổi

```csharp
// ✅ Đúng: invalidate permission cache chỉ khi role permission thay đổi
// Trong RolePermissionService.OnChanged()
protected override async Task OnChanged(RolePermission entity, RolePermissionParam param)
{
    await _permCache.Invalidate(entity.RoleId);
}

// ❌ Sai: invalidate permission cache khi user thay đổi (không liên quan)
// Trong UserService.OnChanged()
protected override async Task OnChanged(User entity, UserParam param)
{
    await _permCache.InvalidateAll(); // quá rộng, tốn kém
}
```

---

## CA-06 — Cache là optimization — logic phải đúng kể cả khi cache trống

```csharp
// ✅ Đúng: loader function luôn có thể fetch từ DB nếu cache miss
var role = await _cache.GetItem(roleId, loader: async () =>
{
    var result = await _db.GetByIdAsync<Role>(roleId);
    return result.IsSuccess ? MapToModel(result.Data) : null;
});

// ❌ Sai: assume cache luôn có data, không có fallback
var role = _cache.TryGet(roleId); // trả null nếu cache trống
var roleName = role.Name;         // NullReferenceException khi cache bị clear
```
