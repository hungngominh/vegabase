# Base Classes

Quy tắc extend và sử dụng `BaseService` và `BaseController`.

## Tổng quan hooks của BaseService

| Hook | Mục đích | Khi nào override |
|---|---|---|
| `ApplyFilter` | Lọc LINQ query | Luôn override để thêm filter theo param |
| `CheckAddCondition` | Validate trước insert | Khi có business rule cho add |
| `CheckUpdateCondition` | Validate trước update | Khi có business rule cho update |
| `ApplyUpdate` | Map param → entity | Khi cần custom mapping |
| `OnChanged` | Sau write thành công | Khi có cache cần invalidate |
| `RefineListData` | Enrich sau khi load | Khi cần join data in-memory |

---

## BC-01 — Service CRUD phải kế thừa `BaseService<TEntity, TModel, TParam>`

```csharp
// ✅ Đúng
public class UserService : BaseService<User, UserModel, UserParam>, IUserService { }

// ❌ Sai: tự implement CRUD từ đầu
public class UserService : IUserService
{
    public async Task<List<UserModel>> GetListAsync(...) { /* viết lại toàn bộ */ }
}
```

---

## BC-02 — Generic constraints phải đúng kiểu

```csharp
// ✅ Đúng
public class UserService : BaseService<User, UserModel, UserParam>
// User : BaseEntity ✓, UserModel : new() ✓, UserParam : BaseParamModel ✓

// ❌ Sai: TModel không có constructor mặc định
public class UserService : BaseService<User, IUserModel, UserParam>
```

---

## BC-03 — Dùng `ApplyFilter` để lọc LINQ, không fetch rồi filter in-memory

```csharp
// ✅ Đúng: filter trên IQueryable
protected override IQueryable<User> ApplyFilter(IQueryable<User> query, UserParam param)
{
    if (!string.IsNullOrEmpty(param.Keyword))
        query = query.Where(u => u.Name.Contains(param.Keyword));
    return query;
}

// ❌ Sai: fetch toàn bộ rồi filter
protected override IQueryable<User> ApplyFilter(IQueryable<User> query, UserParam param)
{
    var all = query.ToList(); // fetch hết
    return all.Where(u => u.Name.Contains(param.Keyword)).AsQueryable();
}
```

---

## BC-04 — Dùng `CheckAddCondition` cho business validation trước insert

```csharp
// ✅ Đúng: 2 tham số (param, sMessage) — không có entity
protected override async Task CheckAddCondition(UserParam param, ServiceMessage sMessage)
{
    var result = await _executor.QueryAsync<User>(q => q.Where(u => u.Email == param.Email && !u.IsDeleted));
    if (result.IsSuccess && result.Data!.Any())
        sMessage += "Email đã được sử dụng.";
}

// ❌ Sai: override Add() để validate, và dùng 3 tham số (entity không tồn tại ở đây)
public override async Task Add(UserParam param, ServiceMessage sMessage)
{
    if (param.Email == "admin@example.com") { sMessage += "Email không hợp lệ."; return; }
    await base.Add(param, sMessage);
}
```

---

## BC-05 — Dùng `OnChanged` để invalidate cache sau write

`OnChanged()` không có tham số và là synchronous. Để biết entity nào vừa thay đổi, lưu Id vào field trước khi hook được gọi.

```csharp
// ✅ Đúng: lưu Id trong ApplyUpdate, dùng trong OnChanged
private Guid _lastChangedId;

protected override void ApplyUpdate(User entity, UserParam param)
{
    _lastChangedId = entity.Id;
    AutoApplyUpdate(entity, param);
}

protected override void OnChanged()
{
    _userCache.Invalidate(_lastChangedId);
}

// ✅ Cũng đúng nếu không cần Id cụ thể
protected override void OnChanged()
{
    _userCache.InvalidateAll();
}

// ❌ Sai: OnChanged không có tham số, không phải async
protected override async Task OnChanged(User entity, UserParam param)
{
    await _userCache.Invalidate(entity.Id); // compile error
}
```

---

## BC-06 — Dùng `param.HasField("FieldName")` cho partial update

> **Lưu ý:** `HasField` trả về `true` khi `UpdatedFields` rỗng (tức là trong các call `Add` thông thường, mọi field đều được coi là "có mặt"). `HasField` chỉ lọc khi `UpdatedFields` được populate — điều này xảy ra tự động qua endpoint `UpdateField`.

```csharp
// ✅ Đúng: chỉ update field được gửi lên (qua UpdateField endpoint)
protected override void ApplyUpdate(User entity, UserParam param)
{
    if (param.HasField("Name")) entity.Name = param.Name;
    if (param.HasField("Email")) entity.Email = param.Email;
}

// ❌ Sai: ghi đè tất cả kể cả field không được gửi
protected override void ApplyUpdate(User entity, UserParam param)
{
    entity.Name = param.Name;   // null nếu client không gửi → xóa mất data
    entity.Email = param.Email;
}
```

---

## BC-07 — Không override core methods (`GetList`, `Add`, `Delete`) trực tiếp

Dùng các hook được cung cấp thay vì override toàn bộ method.

```csharp
// ✅ Đúng: dùng hooks
protected override async Task CheckAddCondition(...) { }
protected override void ApplyUpdate(...) { }

// ❌ Sai: override core method
public override async Task<List<UserModel>> GetList(UserParam param, ServiceMessage sMessage)
{
    // phá vỡ logic pagination, permission check của BaseService
}
```

---

## BC-08 — Dùng `AutoApplyUpdate()` khi tên fields của param và entity trùng nhau

```csharp
// ✅ Đúng: để base tự map khi tên property giống nhau
protected override void ApplyUpdate(User entity, UserParam param)
{
    AutoApplyUpdate(entity, param); // reflection-based mapping
}

// ❌ Sai: map thủ công từng field khi tên giống nhau
protected override void ApplyUpdate(User entity, UserParam param)
{
    if (param.HasField("Name")) entity.Name = param.Name;
    if (param.HasField("Phone")) entity.Phone = param.Phone;
    if (param.HasField("Address")) entity.Address = param.Address;
    // ... 20 field khác giống hệt
}
```

---

## BC-09 — Controller phải kế thừa `BaseController<TService, TModel, TParam>`

```csharp
// ✅ Đúng
[ApiController]
[Route("api/[controller]")]
public class UserController : BaseController<IUserService, UserModel, UserParam>
{
    public UserController(IUserService service) : base(service) { }
}

// ❌ Sai: dùng ControllerBase trực tiếp và tự viết CRUD
[ApiController]
public class UserController : ControllerBase
{
    [HttpGet] public async Task<IActionResult> GetList(...) { /* viết lại */ }
}
```

---

## BC-10 — Dùng `RefineListData` cho enrichment sau load, không query N+1 trong ApplyFilter

```csharp
// ✅ Đúng: batch load related data sau khi có danh sách (GetAll là synchronous)
protected override Task RefineListData(List<UserModel> models, UserParam param, ServiceMessage sMessage)
{
    var roles = _roleCache.GetAll(() => LoadAllRoles());
    foreach (var m in models)
        m.RoleName = roles.FirstOrDefault(r => r.Id == m.RoleId)?.Name;
    return Task.CompletedTask;
}

// ❌ Sai: query trong vòng lặp trong RefineListData gây N+1
protected override async Task RefineListData(List<UserModel> models, UserParam param, ServiceMessage sMessage)
{
    foreach (var m in models) // N+1: 1 query per item
    {
        var roleResult = await _executor.GetByIdAsync<Role>(m.RoleId);
        m.RoleName = roleResult.Data?.Name;
    }
}
```
