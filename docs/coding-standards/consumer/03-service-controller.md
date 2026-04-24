# Service & Controller

Quy tắc kế thừa BaseService và BaseController trong consumer app.

> **Prerequisite:** Đọc [03-base-classes.md](../03-base-classes.md) trước — consumer rules này mở rộng, không lặp lại.

---

## NS-10 — ScreenCode constant class

Consumer tạo `ScreenCodes` static class trong `{App}.Core.Common`:

```csharp
// {App}.Core/Common/ScreenCodes.cs
namespace {App}.Core.Common;

public static class ScreenCodes
{
    // Master list — synced to Screens table on startup
    public static readonly Dictionary<string, string> All = new()
    {
        [PRD_PRODUCT] = "Quản lý sản phẩm",
        [USR_USER]    = "Quản lý người dùng",
        // ... every screen
    };

    public const string PRD_PRODUCT = "PRD_PRODUCT";
    public const string USR_USER    = "USR_USER";
    // ... one const per screen
}
```

**Format:** `MODULE_ENTITY` dạng UPPER_SNAKE (ví dụ: `VHC_VEHICLE`, `USR_USER`, `QUO_REQUEST`). Format này khác với [NS-08](../01-naming.md) (VegaBase nội bộ dùng PascalCase) — consumer dùng UPPER_SNAKE vì code đồng thời là giá trị string lưu trong DB, dễ nhận diện trong log và query hơn PascalCase.

`All` dictionary được DbInitializer dùng để seed Screens table và RoleScreenPermissions khi startup.

---

## BC-11 — Inherit BaseService đúng generic

```csharp
// {App}.Service/Services/Vehicles/VehicleService.cs
using VegaBase.Service.Services;
using VegaBase.Service.Infrastructure.DbActions;
using VegaBase.Service.Permissions;

namespace {App}.Service.Services.Vehicles;

public class VehicleService : BaseService<Vehicle, VehicleModel, VehicleParam>, IVehicleService
{
    public VehicleService(
        IDbActionExecutor executor,
        IPermissionCache permissionCache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<VehicleService> logger)
        : base(executor, permissionCache, httpContextAccessor, logger)
    {
    }
    // ...
}
```

Thứ tự 3 generic: `TEntity` (∈ `{App}.Core.Entities`) → `TModel` → `TParam` (cả hai ∈ `{App}.Service.Models`). Sai thứ tự → compiler error với message generic constraint không rõ ràng.

Constructor phải pass đủ 4 dependency lên `base(...)`.

---

## BC-12 — Override ScreenCode bắt buộc

```csharp
public class VehicleService : BaseService<Vehicle, VehicleModel, VehicleParam>, IVehicleService
{
    protected override string ScreenCode => ScreenCodes.VHC_VEHICLE;
    // ...
}
```

Thiếu → `BaseService` dùng empty string `""` → `CheckPermission` không tìm được screen → mọi request trả 403 mà không có error message rõ ràng. Khó debug vì không có exception, chỉ có `sMessage.HasError = true`.

---

## BC-13 — Dùng hooks đúng mục đích

| Hook | Khi nào dùng | KHÔNG dùng để |
|---|---|---|
| `ApplyFilter` | LINQ filter đồng bộ trên `IQueryable` | Query async cross-table |
| `CheckAddCondition` | Validate duplicate/constraint trước Add | Business logic phức tạp |
| `CheckUpdateCondition` | Validate khi update, kết hợp `HasField` | Filter list |
| `RefineListData` | Post-load enrich (ảnh, join nhỏ) | Filter (đã paginate rồi) |
| `OnChanged` | Invalidate cache sau write | Query DB |

**ApplyFilter** — sync LINQ only:

```csharp
protected override IQueryable<Vehicle> ApplyFilter(IQueryable<Vehicle> query, VehicleParam param)
{
    if (!string.IsNullOrWhiteSpace(param.StatusCode))
        query = query.Where(v => v.StatusCode == param.StatusCode);
    return query.OrderByDescending(v => v.Log_UpdatedDate);
}
```

**Khi cần async cross-table filter** → override `GetListCore` thay vì nhét `await` vào `ApplyFilter`:

```csharp
protected override async Task<List<VehicleModel>> GetListCore(VehicleParam param, ServiceMessage sMessage)
{
    CheckPermission(PermParam(param, "View"), sMessage);
    if (sMessage.HasError) return [];

    // async pre-fetch cross-table
    var specIds = (await _executor.QueryAsync<VehicleSpec>(q =>
        q.Where(s => s.FuelTypeCode == param.FuelTypeCode)))
        .Data!.Select(s => s.VehicleId).ToHashSet();

    var result = await _executor.QueryAsync<Vehicle>(q =>
    {
        var filtered = ApplyFilter(q, param)
            .Where(v => specIds.Contains(v.Id));
        param.TotalCount = filtered.Count();
        return filtered.Skip((param.Page - 1) * param.PageSize).Take(param.PageSize);
    });

    if (!HandleResult(result, sMessage)) return [];
    return result.Data!.Select(ConvertToModel).ToList();
}
```

---

## BC-14 — XParam phải chứa Data property

```csharp
// {App}.Service/Models/Vehicles/VehicleParam.cs
using VegaBase.Service.Models;

namespace {App}.Service.Models.Vehicles;

public class VehicleParam : BaseParamModel
{
    // Payload for Add and UpdateField
    public VehicleModel? Data { get; set; }

    // Filter fields for GetList
    public string? SearchTerm { get; set; }
    public string? StatusCode { get; set; }
    public string? BrandCode  { get; set; }
}
```

`BaseParamModel` cung cấp sẵn: `Id`, `Page`, `PageSize`, `TotalCount`, `CallerUsername`, `CallerRole`, `CallerRoleIds`, `HasField(fieldName)`.

Filter fields và `Data` đặt cùng một `Param` class, không tạo DTO riêng cho từng operation.

---

## BC-15 — Controller inherit BaseController

```csharp
// {App}.API/Controllers/Vehicles/VehicleController.cs
using VegaBase.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace {App}.API.Controllers.Vehicles;

[Authorize]
[Route("api/v1/[controller]")]
public class VehicleController : BaseController<IVehicleService, VehicleModel, VehicleParam>
{
    public VehicleController(IVehicleService service) : base(service) { }
}
```

- `[Authorize]` ở class level — không phải từng action
- Ngoại lệ duy nhất: Auth controller (public endpoint)
- `BaseController` cung cấp sẵn: `GetList`, `GetItem`, `Add`, `UpdateField`, `Delete`
- Xem [BC-09](../03-base-classes.md) cho quy tắc gốc

---

## BC-16 — Custom endpoint trả ApiResponse

Custom `[HttpGet]` / `[HttpPost]` phải wrap response trong `ApiResponse<T>.Ok(...)`:

```csharp
// CORRECT
[HttpGet("Options")]
public IActionResult GetOptions()
{
    var data = _cache.GetAll();
    return Ok(ApiResponse<object>.Ok(new List<object> { data }));
}

// WRONG — raw object, frontend không parse được
[HttpGet("Options")]
public IActionResult GetOptions()
{
    return Ok(new { items = _cache.GetAll() });
}
```

Frontend expect envelope `{ success: bool, data: T[], total: int }`. Plain object break mọi response handler.

Khi cần extend ApiResponse với extra fields:
```csharp
public class ApiResponseWithSuggestions<T, TSuggestion> : ApiResponse<T>
{
    public List<TSuggestion> SuggestedVehicles { get; set; } = [];
}
```

---

## BC-17 — Không inject DbContext vào Service

Service chỉ truy cập DB qua `IDbActionExecutor` (xem [BC-03](../03-base-classes.md)):

```csharp
// WRONG — AppDbContext trong Service constructor
public class VehicleService : BaseService<Vehicle, VehicleModel, VehicleParam>, IVehicleService
{
    private readonly AppDbContext _db; // WRONG

    public VehicleService(IDbActionExecutor executor, ..., AppDbContext db) : base(...)
    {
        _db = db;
    }
}

// CORRECT — AppDbContext chỉ được inject vào Controller khi cần
[Authorize]
[Route("api/v1/[controller]")]
public class VehicleController : BaseController<IVehicleService, VehicleModel, VehicleParam>
{
    private readonly AppDbContext _db;

    public VehicleController(IVehicleService service, AppDbContext db) : base(service)
    {
        _db = db;
    }

    [HttpGet("Options")]
    public async Task<IActionResult> GetOptions()
    {
        var data = await _db.VehicleLookups
            .Where(v => !v.IsDeleted)
            .ToListAsync();
        return Ok(ApiResponse<object>.Ok(new List<object> { data }));
    }
}
```
