# Caching

Quy tắc sử dụng in-memory cache — khi nào cache, khi nào invalidate, và giới hạn của cache.

---

## CA-01 — Chỉ cache dữ liệu ít thay đổi

Cache phù hợp cho: lookup tables, permission lists, category lists, config values.  
Cache không phù hợp cho: user data, search results, transactional data.

```csharp
// ✅ Đúng: cache danh sách role (GetAll là synchronous)
var roles = _roleCache.GetAll(loader: () => LoadRolesFromDb());

// ❌ Sai: cache kết quả tìm kiếm động (không phù hợp dùng cache)
var searchResults = _productCache.GetAll(loader: () => SearchProducts(param.Keyword));
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
// ✅ Đúng: OnChanged là synchronous, không có tham số. Invalidate là void.
private Guid _lastChangedId;

protected override void ApplyUpdate(Role entity, RoleParam param)
{
    _lastChangedId = entity.Id; // lưu Id để dùng trong OnChanged
    AutoApplyUpdate(entity, param);
}

protected override void OnChanged()
{
    _cache.Invalidate(_lastChangedId);
}

// ❌ Sai: OnChanged không có tham số, không phải async
protected override async Task OnChanged(Role entity, RoleParam param)
{
    await _cache.Invalidate(entity.Id); // compile error
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
var cached = _userCache.GetItem(userId, _ => new UserCacheModel
{
    Id = user.Id,
    PasswordHash = user.PasswordHash, // không cache — vi phạm CA-04
    RefreshToken = user.RefreshToken  // không cache — vi phạm CA-04
});
```

---

## CA-05 — Chỉ invalidate `PermissionCache` khi role hoặc permission thực sự thay đổi

```csharp
// ✅ Đúng: invalidate permission cache chỉ khi role permission thay đổi
// Trong RolePermissionService — lưu RoleId trước, dùng trong OnChanged
private Guid _lastRoleId;

protected override void ApplyUpdate(RolePermission entity, RolePermissionParam param)
{
    _lastRoleId = entity.RoleId;
    AutoApplyUpdate(entity, param);
}

protected override void OnChanged()
{
    _permCache.Invalidate(_lastRoleId);
}

// ❌ Sai: không nên InvalidateAll permission cache khi user thay đổi (không liên quan)
// Trong UserService:
protected override void OnChanged()
{
    _permCache.InvalidateAll(); // quá rộng — xóa permission cache của tất cả roles
}
```

---

## CA-06 — Cache là optimization — logic phải đúng kể cả khi cache trống

```csharp
// ✅ Đúng: GetItem là synchronous, loader nhận TKey làm tham số
var role = _cache.GetItem(roleId, loader: key =>
{
    var result = _executor.GetByIdAsync<Role>(key).GetAwaiter().GetResult();
    return result.IsSuccess ? MapToModel(result.Data!) : null;
});

// ❌ Sai: assume cache luôn có data, không có fallback
var role = _cache.GetItem(roleId, loader: _ => null); // loader trả null → cache miss không được xử lý
var roleName = role!.Name; // NullReferenceException khi cache trống
```
