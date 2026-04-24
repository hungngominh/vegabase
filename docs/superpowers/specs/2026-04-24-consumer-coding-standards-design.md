# Consumer Coding Standards — Design Spec

**Date:** 2026-04-24  
**Status:** Approved  
**Audience:** Developer dùng VegaBase NuGet packages để xây dựng ứng dụng (consumer)

---

## 1. Mục tiêu

Bộ quy tắc hiện tại ở `docs/coding-standards/01-08` chỉ áp dụng cho internal VegaBase developers. Spec này mở rộng thêm một bộ quy tắc dành cho **consumer** — các dự án tích hợp `VegaBase.Core`, `VegaBase.Service`, `VegaBase.API` qua NuGet.

Nguồn tham chiếu thực tế: `E:\Work\luxcar-be` (LuxCar), dự án consumer đang dùng VegaBase 1.0.3–1.0.5.

---

## 2. Quyết định kiến trúc

| Quyết định | Lựa chọn | Lý do |
|---|---|---|
| Vị trí docs | `docs/coding-standards/consumer/` (thư mục riêng) | Tách biệt nội bộ vs consumer, không làm phức tạp 01-08 |
| Rule code | Reuse prefix nội bộ (LA-, NS-, DB-, BC-) tiếp số | Consumer rules là extension tự nhiên của internal rules cùng hệ thống |
| Scope | 3 chủ đề: Project Setup, Entity & DbContext, Service/Controller | Permissions/Seeding được bỏ — quá app-specific |

---

## 3. Cấu trúc file

```
docs/coding-standards/
├── README.md                          (cập nhật: thêm section "Consumer" + bảng consumer rule codes)
├── 01-naming.md → 08-testing.md       (giữ nguyên — audience: internal)
└── consumer/
    ├── README.md                      (consumer index: ai đọc, đọc khi nào, map sang internal)
    ├── 01-project-setup.md            (LA-07 → LA-12: project layout, DI, middleware, env, startup)
    ├── 02-entity-dbcontext.md         (NS-09, DB-08 → DB-13: BaseEntity, AppDbContext, migrations)
    └── 03-service-controller.md       (NS-10, BC-11 → BC-17: BaseService, BaseController, hooks)
```

---

## 4. Rules chi tiết

### 4.1 `consumer/01-project-setup.md` — 6 rules

| Code | Tiêu đề |
|---|---|
| LA-07 | Project layout mirror VegaBase |
| LA-08 | DbContext bridge bắt buộc |
| LA-09 | DI đăng ký bắt buộc |
| LA-10 | Request buffering middleware |
| LA-11 | Env vars bắt buộc |
| LA-12 | Startup sequence cố định |

**LA-07 — Project layout mirror VegaBase**  
Consumer phải có tối thiểu 3 project: `{App}.Core` / `{App}.Service` / `{App}.API`. Mỗi project tham chiếu package VegaBase tương ứng (`VegaBase.Core` → `{App}.Core`, v.v.). Có thể thêm project phụ (Storage, Worker) nhưng không được pha trộn trách nhiệm với 3 project chuẩn.

**LA-08 — DbContext bridge bắt buộc**  
Ngay sau `AddDbContext<AppDbContext>()` phải đăng ký:
```csharp
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
```
Lý do: `DbActionExecutor` trong VegaBase inject `DbContext` abstract — nó không biết về `AppDbContext` cụ thể. Thiếu dòng này → `InvalidOperationException` khi resolve `IDbActionExecutor`.

**LA-09 — DI đăng ký bắt buộc**  
Phải đăng ký đủ 5 dependency cốt lõi:
- `builder.Services.AddHttpContextAccessor()` — BaseService cần để đọc JWT claims
- `builder.Services.AddScoped<IDbActionExecutor, DbActionExecutor>()`
- `builder.Services.AddSingleton<IPermissionCache, PermissionCache>()`
- `builder.Services.AddTransient<IJwtHelper, JwtHelper>()`
- `builder.Services.AddSingleton<IPasswordHasher, Argon2idHasher>()`

Thiếu bất kỳ dependency nào → runtime crash khi resolve service đầu tiên.

**LA-10 — Request buffering middleware**  
Phải thêm middleware **trước** `UseAuthentication` (sau Exception middleware):
```csharp
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});
```
Lý do: `BaseController.UpdateField` đọc raw request body 2 lần để detect partial-update fields (`HasField`). Không có `EnableBuffering` → stream đã hết sau lần đọc đầu → `HasField` luôn trả `false` → mọi field đều bị update.

**LA-11 — Env vars bắt buộc**  
`JWT_SECRET` (≥ 32 ký tự) và toàn bộ `DB_*` phải tồn tại khi boot. Chuẩn:
- File `.env` (gitignored) chứa giá trị thật
- File `.env.example` (committed) là template rỗng

Không được cung cấp giá trị fallback cho `JWT_SECRET` trong code. Nếu thiếu → throw `InvalidOperationException` ngay lúc khởi động, không để app chạy với secret yếu/rỗng.

**LA-12 — Startup sequence cố định**  
Thứ tự bắt buộc sau khi build:
1. `db.Database.Migrate()` — apply pending migrations
2. Seed Screens / admin Role / admin User (idempotent)
3. Warm singleton caches
4. Build middleware pipeline

Lý do: seed cần bảng tồn tại (phải sau Migrate); warm cache cần data đã seeded (phải sau seed).

---

### 4.2 `consumer/02-entity-dbcontext.md` — 7 rules

| Code | Tiêu đề |
|---|---|
| NS-09 | Namespace entity theo project |
| DB-08 | Inherit BaseEntity bắt buộc |
| DB-09 | HasQueryFilter trên mọi entity |
| DB-10 | Unique index phải kèm HasFilter |
| DB-11 | Guid PK không auto-generate |
| DB-12 | Override SaveChanges: soft-delete |
| DB-13 | Decimal phải khai báo precision |

**NS-09 — Namespace entity theo project**  
Entity thuộc `{App}.Core.Entities`, không đặt lẫn vào Service hay API. Import `using VegaBase.Core.Entities;` chỉ xuất hiện trong `{App}.Core`.

**DB-08 — Inherit BaseEntity bắt buộc**  
Mọi entity: `public class MyEntity : BaseEntity`. Không tự tạo `Id` (Guid UUIDv7), `IsDeleted` (bool), `Log_CreatedDate/By`, `Log_UpdatedDate/By` — chúng đã có trong `BaseEntity`. Tự tạo → duplicate field, EF migration conflict.

**DB-09 — HasQueryFilter trên mọi entity**  
Mỗi `DbSet` mới phải có trong `OnModelCreating`:
```csharp
modelBuilder.Entity<X>().HasQueryFilter(e => !e.IsDeleted);
```
Thiếu → query bình thường trả về cả bản ghi đã xóa mềm. Ảnh hưởng mọi operation: list, get, duplicate check đều sai.

**DB-10 — Unique index phải kèm HasFilter**  
Mọi unique index trên entity có `IsDeleted` phải dùng partial filter:
```csharp
entity.HasIndex(e => e.Code)
      .IsUnique()
      .HasFilter("\"IsDeleted\" = false");
```
Không có filter → soft-deleted record vẫn chiếm slot unique → lỗi `23505 duplicate key` (PostgreSQL) khi tạo lại record cùng code. Cú pháp `\"IsDeleted\"` là PostgreSQL; SQL Server dùng `[IsDeleted] = 0`.

**DB-11 — Guid PK không auto-generate**  
Trong `OnModelCreating` phải loop tất cả entity:
```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    var idProp = entityType.FindProperty("Id");
    if (idProp != null && idProp.ClrType == typeof(Guid))
        idProp.ValueGenerated = ValueGenerated.Never;
}
```
Lý do Npgsql-specific: PostgreSQL chỉ dùng `IDENTITY` cho `smallint/int/bigint`, không dùng được cho `uuid`. Npgsql mặc định áp `ValueGeneratedOnAdd()` → migration fail với PostgreSQL. ID được generate phía app bởi `Guid.CreateVersion7()` trong `BaseEntity`.

**DB-12 — Override SaveChanges: soft-delete**  
`AppDbContext` phải override cả hai:
```csharp
public override int SaveChanges() { ConvertDeleteToSoftDelete(); return base.SaveChanges(); }
public override Task<int> SaveChangesAsync(...) { ConvertDeleteToSoftDelete(); return base.SaveChangesAsync(...); }
```
Trong đó `ConvertDeleteToSoftDelete` convert `EntityState.Deleted` → `Modified + IsDeleted=true` cho mọi `BaseEntity`. Đây là phòng thủ cuối: tránh `.Remove()` vô tình hard-delete.

**DB-13 — Decimal phải khai báo precision**  
Mọi `decimal` property phải có `.HasPrecision(18, 2)` (hoặc precision phù hợp hơn):
```csharp
b.Property(e => e.PricePerDay).HasPrecision(18, 2);
```
Thiếu → EF tạo cột `numeric` không có precision trên PostgreSQL → warning khi migrate và tiềm ẩn mất dữ liệu thập phân.

---

### 4.3 `consumer/03-service-controller.md` — 8 rules

| Code | Tiêu đề |
|---|---|
| NS-10 | ScreenCode constant class |
| BC-11 | Inherit BaseService đúng generic |
| BC-12 | Override ScreenCode bắt buộc |
| BC-13 | Dùng hooks đúng mục đích |
| BC-14 | XParam phải chứa Data property |
| BC-15 | Controller inherit BaseController |
| BC-16 | Custom endpoint trả ApiResponse |
| BC-17 | Không inject DbContext vào Service |

**NS-10 — ScreenCode constant class**  
Consumer tạo `ScreenCodes` static class trong `{App}.Core.Common`. Format: `MODULE_ENTITY` UPPER_SNAKE (ví dụ: `VHC_VEHICLE`, `USR_USER`). Đồng thời duy trì `Dictionary<string, string> All` để seed vào DB. Xem NS-08 (internal) cho naming rules chi tiết.

**BC-11 — Inherit BaseService đúng generic**  
```csharp
public class VehicleService : BaseService<Vehicle, VehicleModel, VehicleParam>, IVehicleService
```
Ba generic: `TEntity` ∈ `{App}.Core.Entities`, `TModel` + `TParam` ∈ `{App}.Service.Models`. Sai thứ tự hoặc sai project → compiler error với message không rõ ràng.

**BC-12 — Override ScreenCode bắt buộc**  
```csharp
protected override string ScreenCode => ScreenCodes.VHC_VEHICLE;
```
Thiếu → `BaseService` dùng empty string → `CheckPermission` fail silent (không có screen = không có quyền) → mọi request trả 403 mà không có message rõ ràng.

**BC-13 — Dùng hooks đúng mục đích**  

| Hook | Khi nào dùng | KHÔNG dùng để |
|---|---|---|
| `ApplyFilter` | LINQ filter đồng bộ trên `IQueryable` | Query async cross-table |
| `CheckAddCondition` | Validate duplicate/constraint trước Add | Business logic phức tạp |
| `CheckUpdateCondition` | Validate khi update, kết hợp `HasField` | Filter list |
| `RefineListData` | Post-load enrich (ảnh, join nhỏ) | Filter (đã paginate rồi) |
| `OnChanged` | Invalidate cache sau write | Query DB |

Khi filter cần query async cross-table (ví dụ: filter Vehicle theo VehicleSpec) → override `GetListCore`, không nhét `await` vào `ApplyFilter` (ApplyFilter là sync).

**BC-14 — XParam phải chứa Data property**  
```csharp
public class VehicleParam : BaseParamModel
{
    public VehicleModel? Data { get; set; }
    // filter fields for GetList...
}
```
`Data` là payload cho Add/UpdateField. Filter fields và `Data` đặt cùng một `Param` class, không tạo DTO riêng. `BaseParamModel` cung cấp sẵn: `Id`, `Page`, `PageSize`, `TotalCount`, `CallerUsername`, `CallerRole`, `HasField()`.

**BC-15 — Controller inherit BaseController**  
```csharp
[Authorize]
[Route("api/v1/[controller]")]
public class VehicleController : BaseController<IVehicleService, VehicleModel, VehicleParam>
```
`[Authorize]` ở class level. Ngoại lệ duy nhất: Auth controller (public). Tham chiếu BC-09 (internal) cho quy tắc gốc.

**BC-16 — Custom endpoint trả ApiResponse**  
Custom `[HttpGet]` / `[HttpPost]` phải trả `ApiResponse<T>.Ok(...)` hoặc subclass:
```csharp
return Ok(ApiResponse<object>.Ok(new List<object> { data }));
```
Không trả plain object, `OkObjectResult(data)`, hay anonymous object trực tiếp — frontend expect envelope `{ success, data, total }`.

**BC-17 — Không inject DbContext vào Service**  
Service chỉ truy cập DB qua `IDbActionExecutor` (xem BC-03). Khi controller cần query phức tạp ngoài BaseService pattern (ví dụ: dropdown grouped, aggregate) → inject `AppDbContext` trực tiếp vào controller — không đưa `AppDbContext` vào constructor của Service.

---

## 5. README updates

### `docs/coding-standards/README.md`
Thêm section "Audience":
- **Internal developer** (contribute vào VegaBase): đọc 01-08
- **Consumer developer** (dùng VegaBase NuGet): đọc 01-08 (hiểu nguyên tắc) + `consumer/` (cách áp dụng)

Thêm bảng rule codes mới vào master list.

### `docs/coding-standards/consumer/README.md`
Index với:
- Mô tả audience
- Bảng 3 file + 20 rule codes
- Hướng dẫn cross-reference sang internal rules

---

## 6. Không nằm trong scope

- Permissions/Seeding (DbInitializer, ScreenCodes.All seed) — quá app-specific, từng dự án implement khác nhau
- Storage / Worker layer — không phổ quát
- Testing patterns cho consumer app — tách thành spec riêng nếu cần
