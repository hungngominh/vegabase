# Consumer Coding Standards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tạo bộ quy tắc coding standards cho consumer dùng VegaBase NuGet packages, trong thư mục `docs/coding-standards/consumer/` với 21 rules mới mở rộng từ hệ prefix nội bộ (6+7+8).

**Architecture:** 4 file mới trong `consumer/` (README + 3 rule files) và 1 update cho `docs/coding-standards/README.md`. Mỗi file độc lập, viết và commit riêng.

**Tech Stack:** Markdown docs; kiểm tra bằng `grep` để verify rule codes; không có code C# mới.

---

## File Map

| Action | File | Nội dung |
|---|---|---|
| Create | `docs/coding-standards/consumer/README.md` | Consumer index: audience, table of 20 rule codes, cross-reference guide |
| Create | `docs/coding-standards/consumer/01-project-setup.md` | LA-07 → LA-12 (6 rules) |
| Create | `docs/coding-standards/consumer/02-entity-dbcontext.md` | NS-09, DB-08 → DB-13 (7 rules) |
| Create | `docs/coding-standards/consumer/03-service-controller.md` | NS-10, BC-11 → BC-17 (8 rules) |
| Modify | `docs/coding-standards/README.md` | Thêm section Audience + 20 rule codes vào master table |

---

## Task 1: Tạo `consumer/README.md`

**Files:**
- Create: `docs/coding-standards/consumer/README.md`

- [ ] **Bước 1: Tạo file**

```markdown
# Consumer Coding Standards

Bộ quy tắc dành cho developer **dùng VegaBase NuGet packages** để xây dựng ứng dụng.

## Audience

| Bạn là... | Đọc gì |
|---|---|
| Internal developer (contribute vào VegaBase) | Chỉ đọc `../01-08` |
| Consumer developer (dùng VegaBase NuGet) | Đọc `../01-08` (hiểu nguyên tắc) + `consumer/*` (cách áp dụng) |

## Mục lục

| # | File | Rule codes | Nội dung |
|---|---|---|---|
| 01 | [Project Setup](01-project-setup.md) | LA-07 → LA-12 | Project layout, DI wiring, middleware, env vars, startup sequence |
| 02 | [Entity & DbContext](02-entity-dbcontext.md) | NS-09, DB-08 → DB-13 | BaseEntity, AppDbContext, migrations, soft-delete |
| 03 | [Service & Controller](03-service-controller.md) | NS-10, BC-11 → BC-17 | BaseService, BaseController, hooks, Param |

## Tất cả Rule Codes

| Code | Tiêu đề | File |
|---|---|---|
| LA-07 | Project layout mirror VegaBase | [01-project-setup.md](01-project-setup.md) |
| LA-08 | DbContext bridge bắt buộc | [01-project-setup.md](01-project-setup.md) |
| LA-09 | DI đăng ký bắt buộc | [01-project-setup.md](01-project-setup.md) |
| LA-10 | Request buffering middleware | [01-project-setup.md](01-project-setup.md) |
| LA-11 | Env vars bắt buộc | [01-project-setup.md](01-project-setup.md) |
| LA-12 | Startup sequence cố định | [01-project-setup.md](01-project-setup.md) |
| NS-09 | Namespace entity theo project | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-08 | Inherit BaseEntity bắt buộc | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-09 | HasQueryFilter trên mọi entity | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-10 | Unique index phải kèm HasFilter | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-11 | Guid PK không auto-generate | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-12 | Override SaveChanges: soft-delete | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-13 | Decimal phải khai báo precision | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| NS-10 | ScreenCode constant class | [03-service-controller.md](03-service-controller.md) |
| BC-11 | Inherit BaseService đúng generic | [03-service-controller.md](03-service-controller.md) |
| BC-12 | Override ScreenCode bắt buộc | [03-service-controller.md](03-service-controller.md) |
| BC-13 | Dùng hooks đúng mục đích | [03-service-controller.md](03-service-controller.md) |
| BC-14 | XParam phải chứa Data property | [03-service-controller.md](03-service-controller.md) |
| BC-15 | Controller inherit BaseController | [03-service-controller.md](03-service-controller.md) |
| BC-16 | Custom endpoint trả ApiResponse | [03-service-controller.md](03-service-controller.md) |
| BC-17 | Không inject DbContext vào Service | [03-service-controller.md](03-service-controller.md) |

## Cross-reference sang internal rules

Các consumer rules **mở rộng** internal rules — không lặp lại. Khi đọc consumer rule, tham chiếu về internal rule nền:

| Consumer rules | Mở rộng internal rules |
|---|---|
| LA-07 → LA-12 | [LA-01 → LA-06](../02-architecture.md) — dependency flow, layer responsibilities |
| DB-08 → DB-13 | [DB-01 → DB-07](../06-database.md) — soft delete, audit, transactions |
| BC-11 → BC-17 | [BC-01 → BC-10](../03-base-classes.md) — BaseService & BaseController patterns |
| NS-09, NS-10 | [NS-01 → NS-08](../01-naming.md) — naming conventions |
```

- [ ] **Bước 2: Verify**

```bash
grep -c "LA-07\|LA-08\|BC-17\|DB-13" docs/coding-standards/consumer/README.md
```

Expected: `4` (4 dòng chứa các rule code đầu/cuối của từng nhóm)

- [ ] **Bước 3: Commit**

```bash
git add docs/coding-standards/consumer/README.md
git commit -m "docs: add consumer coding standards index (21 rule codes)"
```

---

## Task 2: Tạo `consumer/01-project-setup.md` (LA-07–LA-12)

**Files:**
- Create: `docs/coding-standards/consumer/01-project-setup.md`

- [ ] **Bước 1: Tạo file**

```markdown
# Project Setup

Quy tắc thiết lập project cho consumer dùng VegaBase NuGet packages.

> **Prerequisite:** Đọc [02-architecture.md](../02-architecture.md) để hiểu dependency flow trước.

---

## LA-07 — Project layout mirror VegaBase

Consumer phải có tối thiểu 3 project:

| Project | NuGet reference | Trách nhiệm |
|---|---|---|
| `{App}.Core` | `VegaBase.Core` | Entities, DbContext, constants |
| `{App}.Service` | `VegaBase.Service` | Services, Models, DTOs |
| `{App}.API` | `VegaBase.API` | Controllers, middleware, Program.cs |

```xml
<!-- {App}.API/{App}.API.csproj -->
<ItemGroup>
  <PackageReference Include="VegaBase.API"     Version="x.x.x" />
  <PackageReference Include="VegaBase.Core"    Version="x.x.x" />
  <PackageReference Include="VegaBase.Service" Version="x.x.x" />
</ItemGroup>
```

Có thể thêm project phụ (`{App}.Storage`, `{App}.Worker`) nhưng không được pha trộn trách nhiệm với 3 project chuẩn.

**Vi phạm:**
```
// WRONG — Entity trong Service project
LuxCar.Service/Entities/Vehicle.cs

// CORRECT — Entity đúng chỗ
LuxCar.Core/Entities/Vehicle.cs
```

---

## LA-08 — DbContext bridge bắt buộc

Ngay sau `AddDbContext<AppDbContext>()` phải đăng ký bridge:

```csharp
// Program.cs
if (isPostgres)
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));
else
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));

// REQUIRED: bridge abstract DbContext → AppDbContext
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
```

**Tại sao:** `DbActionExecutor` trong VegaBase inject `DbContext` abstract — nó không biết về `AppDbContext` cụ thể. Thiếu dòng bridge → `InvalidOperationException` khi resolve `IDbActionExecutor`:

```
Unable to resolve service for type 'Microsoft.EntityFrameworkCore.DbContext'
```

---

## LA-09 — DI đăng ký bắt buộc

Phải đăng ký đủ 5 dependency cốt lõi của VegaBase:

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDbActionExecutor, DbActionExecutor>();
builder.Services.AddSingleton<IPermissionCache, PermissionCache>();
builder.Services.AddTransient<IJwtHelper, VegaBase.API.Infrastructure.JwtHelper>();
builder.Services.AddSingleton<IPasswordHasher, Argon2idHasher>();
```

| Dependency | Lifetime | Lý do |
|---|---|---|
| `IHttpContextAccessor` | (framework) | BaseService đọc JWT claims từ HTTP context |
| `IDbActionExecutor` | Scoped | Một DB session per HTTP request |
| `IPermissionCache` | Singleton | Cache sống suốt app lifetime |
| `IJwtHelper` | Transient | Stateless, tạo mới mỗi lần dùng |
| `IPasswordHasher` | Singleton | Stateless, Argon2id không có state |

Thiếu bất kỳ dependency nào → runtime crash khi resolve service đầu tiên của request.

---

## LA-10 — Request buffering middleware

Phải thêm middleware **trước** `UseAuthentication` (sau Exception middleware):

```csharp
// Program.cs
app.UseMiddleware<ExceptionHandlingMiddleware>();

// REQUIRED: enable request body re-read for partial-update detection
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

**Tại sao:** `BaseController.UpdateField` đọc raw request body 2 lần — lần 1 để deserialize, lần 2 để detect partial-update fields qua `HasField`. Không có `EnableBuffering` → stream hết sau lần đọc đầu → `HasField` luôn trả `false` → mọi field đều bị update kể cả field không gửi lên.

---

## LA-11 — Env vars bắt buộc

`JWT_SECRET` (≥ 32 ký tự) và toàn bộ `DB_*` phải tồn tại khi boot.

**Chuẩn:**
```bash
# .env (gitignored — giá trị thật)
JWT_SECRET=your-random-secret-minimum-32-characters
DB_IS_POSTGRESQL=true
DB_HOST=localhost
DB_PORT=5432
DB_NAME=AppDB
DB_USER=postgres
DB_PASSWORD=your_password

# .env.example (committed — template rỗng)
JWT_SECRET=
DB_IS_POSTGRESQL=true
DB_HOST=localhost
DB_PORT=5432
DB_NAME=AppDB
DB_USER=postgres
DB_PASSWORD=
```

**Không được** cung cấp fallback cho `JWT_SECRET` trong code:

```csharp
// WRONG — fallback yếu, app chạy với secret rỗng
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "default";

// CORRECT — fail fast nếu thiếu
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? throw new InvalidOperationException("JWT_SECRET is required in .env");
```

---

## LA-12 — Startup sequence cố định

Thứ tự bắt buộc sau `app.Build()`:

```csharp
var app = builder.Build();

// 1. Apply pending EF migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// 2. Seed essential data (idempotent)
await DbInitializer.SeedAsync(app.Services);

// 3. Warm singleton caches (after seed, before first request)
app.Services.GetRequiredService<IPermissionCache>().Warm();
// warm other singleton caches here...

// 4. Middleware pipeline
app.UseCors();
app.UseHttpsRedirection();
// ...
app.Run();
```

**Lý do thứ tự:**
- Seed trước Migrate → bảng chưa tồn tại → exception
- Warm trước Seed → cache thiếu data mới seeded → stale cache ngay từ start
```

- [ ] **Bước 2: Verify**

```bash
grep -c "## LA-0[7-9]\|## LA-1[0-2]" docs/coding-standards/consumer/01-project-setup.md
```

Expected: `6` (6 headings LA-07 đến LA-12)

- [ ] **Bước 3: Commit**

```bash
git add docs/coding-standards/consumer/01-project-setup.md
git commit -m "docs: add consumer/01-project-setup.md (LA-07 to LA-12)"
```

---

## Task 3: Tạo `consumer/02-entity-dbcontext.md` (NS-09, DB-08–DB-13)

**Files:**
- Create: `docs/coding-standards/consumer/02-entity-dbcontext.md`

- [ ] **Bước 1: Tạo file**

```markdown
# Entity & DbContext

Quy tắc định nghĩa entity và cấu hình AppDbContext cho consumer dùng VegaBase.

> **Prerequisite:** Đọc [06-database.md](../06-database.md) cho quy tắc soft-delete và audit fields.

---

## NS-09 — Namespace entity theo project

Entity thuộc `{App}.Core.Entities`. Import `using VegaBase.Core.Entities;` chỉ xuất hiện trong `{App}.Core`.

```csharp
// CORRECT — LuxCar.Core/Entities/Vehicle.cs
using VegaBase.Core.Entities;
namespace LuxCar.Core.Entities;

public class Vehicle : BaseEntity { ... }
```

```csharp
// WRONG — entity trong Service project
namespace LuxCar.Service.Entities;
public class Vehicle : BaseEntity { ... }
```

---

## DB-08 — Inherit BaseEntity bắt buộc

Mọi entity phải kế thừa `BaseEntity`:

```csharp
using VegaBase.Core.Entities;
namespace {App}.Core.Entities;

public class Vehicle : BaseEntity
{
    public string Name         { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    // ... domain fields only
}
```

`BaseEntity` cung cấp sẵn:
- `Id` — `Guid`, tự set bằng `Guid.CreateVersion7()` khi tạo mới
- `IsDeleted` — `bool`, dùng cho soft delete
- `Log_CreatedDate`, `Log_CreatedBy` — audit created
- `Log_UpdatedDate`, `Log_UpdatedBy` — audit updated

**Không tự khai báo lại các field trên** → EF migration conflict, duplicate column.

---

## DB-09 — HasQueryFilter trên mọi entity

Mỗi `DbSet` mới phải có global query filter trong `OnModelCreating`:

```csharp
// {App}.Core/Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Apply to EVERY entity
    modelBuilder.Entity<Vehicle>()      .HasQueryFilter(e => !e.IsDeleted);
    modelBuilder.Entity<VehicleSpec>()  .HasQueryFilter(e => !e.IsDeleted);
    modelBuilder.Entity<VehicleImage>() .HasQueryFilter(e => !e.IsDeleted);
    // ... every DbSet
}
```

Thiếu filter → query bình thường trả về cả bản ghi đã xóa mềm. Ảnh hưởng mọi operation: list, get, duplicate check đều sai.

Để query không có filter (ví dụ: seed, admin check): dùng `.IgnoreQueryFilters()`.

---

## DB-10 — Unique index phải kèm HasFilter

Mọi unique index trên entity phải dùng partial filter:

```csharp
// PostgreSQL syntax (Npgsql)
modelBuilder.Entity<User>(b =>
{
    b.HasIndex(u => u.Username)
     .IsUnique()
     .HasFilter("\"IsDeleted\" = false");
});

// SQL Server syntax
modelBuilder.Entity<User>(b =>
{
    b.HasIndex(u => u.Username)
     .IsUnique()
     .HasFilter("[IsDeleted] = 0");
});
```

**Tại sao:** Soft-deleted record vẫn chiếm slot trong index thường. Khi tạo lại record cùng username → lỗi `23505 duplicate key` (PostgreSQL) dù record cũ đã `IsDeleted=true`.

Với composite unique index:
```csharp
b.HasIndex(e => new { e.VehicleId, e.TagType, e.TagCode })
 .IsUnique()
 .HasFilter("\"IsDeleted\" = false");
```

---

## DB-11 — Guid PK không auto-generate

Trong `OnModelCreating` phải tắt auto-generation cho mọi Guid PK:

```csharp
// {App}.Core/Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Npgsql fix: PostgreSQL cannot use IDENTITY for uuid columns
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        var idProp = entityType.FindProperty("Id");
        if (idProp != null && idProp.ClrType == typeof(Guid))
            idProp.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
    }

    // ... rest of OnModelCreating
}
```

**Lý do:** Npgsql mặc định áp `ValueGeneratedOnAdd()` cho mọi PK. PostgreSQL chỉ hỗ trợ `IDENTITY` cho `smallint/int/bigint`, không cho `uuid` → migration fail. Convention loop này tự động áp cho tất cả entity hiện tại và tương lai.

ID được generate phía app: `BaseEntity` tự gọi `Guid.CreateVersion7()` trong constructor.

---

## DB-12 — Override SaveChanges: soft-delete

`AppDbContext` phải override cả hai `SaveChanges` để convert hard-delete thành soft-delete:

```csharp
// {App}.Core/Data/AppDbContext.cs
public override int SaveChanges()
{
    ConvertDeleteToSoftDelete();
    return base.SaveChanges();
}

public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    ConvertDeleteToSoftDelete();
    return base.SaveChangesAsync(cancellationToken);
}

private void ConvertDeleteToSoftDelete()
{
    var deleted = ChangeTracker
        .Entries<VegaBase.Core.Entities.BaseEntity>()
        .Where(e => e.State == EntityState.Deleted);

    foreach (var entry in deleted)
    {
        entry.State            = EntityState.Modified;
        entry.Entity.IsDeleted = true;
    }
}
```

Đây là phòng thủ cuối: tránh `.Remove()` vô tình hard-delete. Services cũng set `IsDeleted` thủ công trước, nhưng lớp này đảm bảo không ai bypass.

---

## DB-13 — Decimal phải khai báo precision

Mọi `decimal` property phải khai báo precision trong `OnModelCreating`:

```csharp
modelBuilder.Entity<VehicleRentalConfig>(b =>
{
    b.Property(e => e.Deposit).HasPrecision(18, 2);
});

modelBuilder.Entity<VehicleServiceTypePrice>(b =>
{
    b.Property(e => e.PricePerDay).HasPrecision(18, 2);
    b.Property(e => e.OriginalPrice).HasPrecision(18, 2);
});
```

Thiếu → EF tạo cột `numeric` không có precision trên PostgreSQL → EF warning `No store type was specified for the decimal property` + tiềm ẩn mất dữ liệu thập phân khi precision thay đổi ở migration sau.
```

- [ ] **Bước 2: Verify**

```bash
grep -c "## NS-09\|## DB-08\|## DB-09\|## DB-10\|## DB-11\|## DB-12\|## DB-13" docs/coding-standards/consumer/02-entity-dbcontext.md
```

Expected: `7`

- [ ] **Bước 3: Commit**

```bash
git add docs/coding-standards/consumer/02-entity-dbcontext.md
git commit -m "docs: add consumer/02-entity-dbcontext.md (NS-09, DB-08 to DB-13)"
```

---

## Task 4: Tạo `consumer/03-service-controller.md` (NS-10, BC-11–BC-17)

**Files:**
- Create: `docs/coding-standards/consumer/03-service-controller.md`

- [ ] **Bước 1: Tạo file**

```markdown
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

**Format:** `MODULE_ENTITY` dạng UPPER_SNAKE. Xem [NS-08](../01-naming.md) cho naming rules chi tiết.

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
```

- [ ] **Bước 2: Verify**

```bash
grep -c "## NS-10\|## BC-11\|## BC-12\|## BC-13\|## BC-14\|## BC-15\|## BC-16\|## BC-17" docs/coding-standards/consumer/03-service-controller.md
```

Expected: `8`

- [ ] **Bước 3: Commit**

```bash
git add docs/coding-standards/consumer/03-service-controller.md
git commit -m "docs: add consumer/03-service-controller.md (NS-10, BC-11 to BC-17)"
```

---

## Task 5: Cập nhật `docs/coding-standards/README.md`

**Files:**
- Modify: `docs/coding-standards/README.md`

- [ ] **Bước 1: Thêm section Audience sau dòng mô tả đầu**

Tìm đoạn:
```markdown
Bộ quy tắc lập trình cho VegaBase — áp dụng cho developer mới, team nội bộ, và người tích hợp thư viện.

## Cách sử dụng
```

Thêm section Audience ở giữa:
```markdown
Bộ quy tắc lập trình cho VegaBase — áp dụng cho developer mới, team nội bộ, và người tích hợp thư viện.

## Audience

| Bạn là... | Đọc gì |
|---|---|
| Internal developer (contribute vào VegaBase) | Đọc 01-08 bên dưới |
| Consumer developer (dùng VegaBase NuGet) | Đọc 01-08 (hiểu nguyên tắc) + [consumer/](consumer/README.md) (cách áp dụng) |

## Cách sử dụng
```

- [ ] **Bước 2: Thêm dòng Consumer vào bảng Mục lục**

Tìm cuối bảng Mục lục (sau dòng `08-testing.md`):
```markdown
| 08 | [Testing](08-testing.md) | Testing standards |
```

Thêm:
```markdown
| 08 | [Testing](08-testing.md) | Testing standards |
| — | **[Consumer Rules](consumer/README.md)** | **Quy tắc cho dự án dùng VegaBase NuGet** |
```

- [ ] **Bước 3: Thêm 20 rule codes vào bảng "Tất cả Rule Codes"**

Tìm cuối bảng (sau dòng cuối cùng `TS-05`):
```markdown
| TS-05 | Test project structure | [08-testing.md](08-testing.md) |
```

Thêm:
```markdown
| TS-05 | Test project structure | [08-testing.md](08-testing.md) |
| **Consumer Rules** | | |
| LA-07 | Project layout mirror VegaBase | [consumer/01-project-setup.md](consumer/01-project-setup.md) |
| LA-08 | DbContext bridge bắt buộc | [consumer/01-project-setup.md](consumer/01-project-setup.md) |
| LA-09 | DI đăng ký bắt buộc | [consumer/01-project-setup.md](consumer/01-project-setup.md) |
| LA-10 | Request buffering middleware | [consumer/01-project-setup.md](consumer/01-project-setup.md) |
| LA-11 | Env vars bắt buộc | [consumer/01-project-setup.md](consumer/01-project-setup.md) |
| LA-12 | Startup sequence cố định | [consumer/01-project-setup.md](consumer/01-project-setup.md) |
| NS-09 | Namespace entity theo project | [consumer/02-entity-dbcontext.md](consumer/02-entity-dbcontext.md) |
| DB-08 | Inherit BaseEntity bắt buộc | [consumer/02-entity-dbcontext.md](consumer/02-entity-dbcontext.md) |
| DB-09 | HasQueryFilter trên mọi entity | [consumer/02-entity-dbcontext.md](consumer/02-entity-dbcontext.md) |
| DB-10 | Unique index phải kèm HasFilter | [consumer/02-entity-dbcontext.md](consumer/02-entity-dbcontext.md) |
| DB-11 | Guid PK không auto-generate | [consumer/02-entity-dbcontext.md](consumer/02-entity-dbcontext.md) |
| DB-12 | Override SaveChanges: soft-delete | [consumer/02-entity-dbcontext.md](consumer/02-entity-dbcontext.md) |
| DB-13 | Decimal phải khai báo precision | [consumer/02-entity-dbcontext.md](consumer/02-entity-dbcontext.md) |
| NS-10 | ScreenCode constant class | [consumer/03-service-controller.md](consumer/03-service-controller.md) |
| BC-11 | Inherit BaseService đúng generic | [consumer/03-service-controller.md](consumer/03-service-controller.md) |
| BC-12 | Override ScreenCode bắt buộc | [consumer/03-service-controller.md](consumer/03-service-controller.md) |
| BC-13 | Dùng hooks đúng mục đích | [consumer/03-service-controller.md](consumer/03-service-controller.md) |
| BC-14 | XParam phải chứa Data property | [consumer/03-service-controller.md](consumer/03-service-controller.md) |
| BC-15 | Controller inherit BaseController | [consumer/03-service-controller.md](consumer/03-service-controller.md) |
| BC-16 | Custom endpoint trả ApiResponse | [consumer/03-service-controller.md](consumer/03-service-controller.md) |
| BC-17 | Không inject DbContext vào Service | [consumer/03-service-controller.md](consumer/03-service-controller.md) |
```

- [ ] **Bước 4: Verify**

```bash
grep -c "LA-07\|LA-12\|BC-17\|DB-13\|Consumer Rules" docs/coding-standards/README.md
```

Expected: `5` (mỗi pattern xuất hiện ít nhất 1 lần)

- [ ] **Bước 5: Commit**

```bash
git add docs/coding-standards/README.md
git commit -m "docs: update coding-standards README with Consumer audience + 20 rule codes"
```

---

## Task 6: Verify toàn bộ

- [ ] **Bước 1: Kiểm tra tất cả file tồn tại**

```bash
ls docs/coding-standards/consumer/
```

Expected output:
```
README.md
01-project-setup.md
02-entity-dbcontext.md
03-service-controller.md
```

- [ ] **Bước 2: Kiểm tra tổng số rule codes**

```bash
grep -cE "^\| (LA|NS|DB|BC)-[0-9]" docs/coding-standards/consumer/README.md
```

Expected: `21`

- [ ] **Bước 3: Kiểm tra không có rule nào bị trùng với internal**

```bash
grep "^| LA-0[1-6]\|^| NS-0[1-8]\b\|^| DB-0[1-7]\b\|^| BC-0[1-9]\b\|^| BC-10" docs/coding-standards/consumer/README.md
```

Expected: no output (không có rule nội bộ bị copy vào consumer)

- [ ] **Bước 4: Final commit**

```bash
git log --oneline -6
```

Expected: 5 commit mới từ Tasks 1-5 hiển thị ở đầu.
