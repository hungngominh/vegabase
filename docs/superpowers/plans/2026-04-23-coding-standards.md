# VegaBase Coding Standards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tạo bộ quy tắc lập trình (coding standards) đa ngôn ngữ cho VegaBase gồm 9 file markdown có chỉ mục, phục vụ onboarding, team review, và người dùng NuGet.

**Architecture:** Multi-file structure tại `docs/coding-standards/`. Mỗi file phụ trách một nhóm quy tắc độc lập với mã rule dạng `PREFIX-NN`. File `README.md` là chỉ mục tổng hợp toàn bộ rule codes với link dẫn đến từng file.

**Tech Stack:** Markdown, Git.

---

## File Map

| File | Tạo mới / Sửa | Mô tả |
|---|---|---|
| `docs/coding-standards/README.md` | Tạo mới | Chỉ mục tổng hợp |
| `docs/coding-standards/01-naming.md` | Tạo mới | NS-01 → NS-08 |
| `docs/coding-standards/02-architecture.md` | Tạo mới | LA-01 → LA-06 |
| `docs/coding-standards/03-base-classes.md` | Tạo mới | BC-01 → BC-10 |
| `docs/coding-standards/04-error-handling.md` | Tạo mới | EH-01 → EH-06 |
| `docs/coding-standards/05-security.md` | Tạo mới | SEC-01 → SEC-06 |
| `docs/coding-standards/06-database.md` | Tạo mới | DB-01 → DB-07 |
| `docs/coding-standards/07-caching.md` | Tạo mới | CA-01 → CA-06 |
| `docs/coding-standards/08-testing.md` | Tạo mới | TS-01 → TS-05 |

---

## Task 1: Tạo thư mục và file README (chỉ mục)

**Files:**
- Create: `docs/coding-standards/README.md`

- [ ] **Step 1: Tạo thư mục**

```bash
mkdir -p docs/coding-standards
```

- [ ] **Step 2: Viết README.md**

Tạo file `docs/coding-standards/README.md` với nội dung:

````markdown
# VegaBase Coding Standards

Bộ quy tắc lập trình cho VegaBase — áp dụng cho developer mới, team nội bộ, và người tích hợp thư viện.

## Cách sử dụng

- Khi review PR: tham chiếu mã rule trong comment (ví dụ: `vi phạm NS-03`)
- Khi onboarding: đọc tuần tự từ 01 → 08
- Khi tích hợp NuGet: đọc 02, 03, 04

## Mục lục

| # | File | Nội dung |
|---|---|---|
| 01 | [Naming Conventions](01-naming.md) | Đặt tên namespace, class, interface, field |
| 02 | [Layer Architecture](02-architecture.md) | Dependency flow, trách nhiệm từng layer |
| 03 | [Base Classes](03-base-classes.md) | Cách extend BaseService & BaseController |
| 04 | [Error Handling](04-error-handling.md) | ServiceMessage, DbResult, exceptions |
| 05 | [Security](05-security.md) | Password, JWT, RBAC |
| 06 | [Database](06-database.md) | Soft delete, audit, transactions |
| 07 | [Caching](07-caching.md) | Cache rules và invalidation |
| 08 | [Testing](08-testing.md) | Testing standards |

## Tất cả Rule Codes

| Code | Tiêu đề | File |
|---|---|---|
| NS-01 | Namespace pattern | [01-naming.md](01-naming.md) |
| NS-02 | Entity naming (no suffix) | [01-naming.md](01-naming.md) |
| NS-03 | Model suffix | [01-naming.md](01-naming.md) |
| NS-04 | Param suffix + BaseParamModel | [01-naming.md](01-naming.md) |
| NS-05 | Interface I prefix | [01-naming.md](01-naming.md) |
| NS-06 | Audit fields Log_ prefix | [01-naming.md](01-naming.md) |
| NS-07 | Async suffix | [01-naming.md](01-naming.md) |
| NS-08 | Screen codes PascalCase | [01-naming.md](01-naming.md) |
| LA-01 | Dependency flow một chiều | [02-architecture.md](02-architecture.md) |
| LA-02 | Core không có EF Core | [02-architecture.md](02-architecture.md) |
| LA-03 | Service không có HTTP types | [02-architecture.md](02-architecture.md) |
| LA-04 | Business logic chỉ trong Service | [02-architecture.md](02-architecture.md) |
| LA-05 | API chỉ là HTTP wiring | [02-architecture.md](02-architecture.md) |
| LA-06 | Entity mới phải định nghĩa trong Core | [02-architecture.md](02-architecture.md) |
| BC-01 | Extend BaseService cho CRUD | [03-base-classes.md](03-base-classes.md) |
| BC-02 | Generic constraints đúng | [03-base-classes.md](03-base-classes.md) |
| BC-03 | ApplyFilter cho LINQ | [03-base-classes.md](03-base-classes.md) |
| BC-04 | CheckAddCondition cho validation | [03-base-classes.md](03-base-classes.md) |
| BC-05 | OnChanged cho cache invalidation | [03-base-classes.md](03-base-classes.md) |
| BC-06 | HasField cho partial update | [03-base-classes.md](03-base-classes.md) |
| BC-07 | Không override core methods | [03-base-classes.md](03-base-classes.md) |
| BC-08 | AutoApplyUpdate khi fields match | [03-base-classes.md](03-base-classes.md) |
| BC-09 | Controller extend BaseController | [03-base-classes.md](03-base-classes.md) |
| BC-10 | RefineListData cho post-load | [03-base-classes.md](03-base-classes.md) |
| EH-01 | ServiceMessage cho business errors | [04-error-handling.md](04-error-handling.md) |
| EH-02 | Tích lũy errors với += | [04-error-handling.md](04-error-handling.md) |
| EH-03 | Kiểm tra DbResult.IsSuccess | [04-error-handling.md](04-error-handling.md) |
| EH-04 | Không dùng exception cho business flow | [04-error-handling.md](04-error-handling.md) |
| EH-05 | Không swallow exceptions | [04-error-handling.md](04-error-handling.md) |
| EH-06 | Không expose nội bộ ra client | [04-error-handling.md](04-error-handling.md) |
| SEC-01 | Permission qua IPermissionCache | [05-security.md](05-security.md) |
| SEC-02 | [Authorize] trên tất cả controllers | [05-security.md](05-security.md) |
| SEC-03 | Chỉ dùng IPasswordHasher | [05-security.md](05-security.md) |
| SEC-04 | JWT_SECRET từ env var | [05-security.md](05-security.md) |
| SEC-05 | Không log sensitive data | [05-security.md](05-security.md) |
| SEC-06 | Không trả sensitive data trong response | [05-security.md](05-security.md) |
| DB-01 | Chỉ dùng SoftDeleteAsync | [06-database.md](06-database.md) |
| DB-02 | Không set audit fields thủ công | [06-database.md](06-database.md) |
| DB-03 | Multi-entity dùng UnitOfWork | [06-database.md](06-database.md) |
| DB-04 | Single-entity dùng DbActionExecutor | [06-database.md](06-database.md) |
| DB-05 | Raw SQL phải có comment | [06-database.md](06-database.md) |
| DB-06 | Filter IsDeleted trong ApplyFilter | [06-database.md](06-database.md) |
| DB-07 | Primary key là UUIDv7 từ BaseEntity | [06-database.md](06-database.md) |
| CA-01 | Chỉ cache dữ liệu ít thay đổi | [07-caching.md](07-caching.md) |
| CA-02 | Dùng ICacheStore<K,V> | [07-caching.md](07-caching.md) |
| CA-03 | Invalidate qua OnChanged | [07-caching.md](07-caching.md) |
| CA-04 | Không cache sensitive data | [07-caching.md](07-caching.md) |
| CA-05 | PermissionCache invalidation đúng lúc | [07-caching.md](07-caching.md) |
| CA-06 | Cache là optimization, không phải dependency | [07-caching.md](07-caching.md) |
| TS-01 | Unit test cho business logic hooks | [08-testing.md](08-testing.md) |
| TS-02 | Integration test dùng real DB | [08-testing.md](08-testing.md) |
| TS-03 | Test naming convention | [08-testing.md](08-testing.md) |
| TS-04 | AAA pattern, một assertion chính | [08-testing.md](08-testing.md) |
| TS-05 | Test project structure | [08-testing.md](08-testing.md) |
````

- [ ] **Step 3: Commit**

```bash
git add docs/coding-standards/README.md
git commit -m "docs: add coding standards index (README)"
```

---

## Task 2: Viết 01-naming.md

**Files:**
- Create: `docs/coding-standards/01-naming.md`

- [ ] **Step 1: Viết file**

Tạo file `docs/coding-standards/01-naming.md` với nội dung:

````markdown
# Naming Conventions

Quy tắc đặt tên cho toàn bộ codebase VegaBase.

---

## NS-01 — Namespace phải theo pattern `VegaBase.[Layer].[SubDomain]`

Namespace phản ánh đúng vị trí của file trong kiến trúc 3 lớp.

```csharp
// ✅ Đúng
namespace VegaBase.Service.Infrastructure.Cache;
namespace VegaBase.Core.Entities;
namespace VegaBase.API.Controllers;

// ❌ Sai
namespace Cache;
namespace VegaBase.CacheService;
namespace Services.VegaBase.User;
```

---

## NS-02 — Entity class không có suffix

Entity là domain object — tên đã đủ mô tả, không cần thêm `Entity`, `Record`, `Table`.

```csharp
// ✅ Đúng
public class User : BaseEntity { }
public class Product : BaseEntity { }

// ❌ Sai
public class UserEntity : BaseEntity { }
public class ProductRecord : BaseEntity { }
```

---

## NS-03 — Model (DTO) phải có suffix `Model`

Model là object truyền dữ liệu ra ngoài service — luôn kết thúc bằng `Model`.

```csharp
// ✅ Đúng
public class UserModel { }
public class ProductModel { }

// ❌ Sai
public class UserDto { }
public class UserResponse { }
public class UserData { }
```

---

## NS-04 — Parameter class phải có suffix `Param` và kế thừa `BaseParamModel`

Tất cả param truyền vào service/controller phải extend `BaseParamModel`.

```csharp
// ✅ Đúng
public class UserParam : BaseParamModel { }
public class ProductParam : BaseParamModel { }

// ❌ Sai
public class CreateUserRequest { }
public class UserInput : BaseParamModel { }  // suffix sai
public class UserParam { }                   // thiếu kế thừa
```

---

## NS-05 — Interface phải có prefix `I`

```csharp
// ✅ Đúng
public interface IUserService { }
public interface IPermissionCache { }

// ❌ Sai
public interface UserService { }
public interface UserServiceInterface { }
```

---

## NS-06 — Audit fields phải có prefix `Log_`

Các field audit trên entity phải có prefix `Log_` để phân biệt với business fields.

```csharp
// ✅ Đúng
public DateTimeOffset Log_CreatedDate { get; set; }
public string? Log_CreatedBy { get; set; }
public DateTimeOffset? Log_UpdatedDate { get; set; }
public string? Log_UpdatedBy { get; set; }

// ❌ Sai
public DateTimeOffset CreatedDate { get; set; }
public string? Audit_CreatedBy { get; set; }
public DateTimeOffset? ModifiedAt { get; set; }
```

---

## NS-07 — Method bất đồng bộ phải có suffix `Async`

```csharp
// ✅ Đúng
public async Task<UserModel> GetByEmailAsync(string email) { }
public async Task<DbResult<User>> AddAsync(User entity) { }

// ❌ Sai
public async Task<UserModel> GetByEmail(string email) { }
public async Task<DbResult<User>> Add(User entity) { }
```

---

## NS-08 — Screen codes phải là PascalCase mô tả rõ chức năng

Screen code dùng trong permission check — phải đủ rõ khi đọc trong log.

```csharp
// ✅ Đúng
"UserManagement"
"RolePermission"
"ProductCatalog"

// ❌ Sai
"user_management"
"USER_MANAGEMENT"
"usermgmt"
"Screen1"
```
````

- [ ] **Step 2: Commit**

```bash
git add docs/coding-standards/01-naming.md
git commit -m "docs: add naming conventions (NS-01 to NS-08)"
```

---

## Task 3: Viết 02-architecture.md

**Files:**
- Create: `docs/coding-standards/02-architecture.md`

- [ ] **Step 1: Viết file**

Tạo file `docs/coding-standards/02-architecture.md` với nội dung:

````markdown
# Layer Architecture

Quy tắc về dependency flow và trách nhiệm giữa các lớp trong VegaBase.

## Tổng quan kiến trúc

```
VegaBase.API          (Presentation)
      ↓ phụ thuộc vào
VegaBase.Service      (Business Logic)
      ↓ phụ thuộc vào
VegaBase.Core         (Domain Models)
```

Chiều phụ thuộc là **một chiều xuống**. Layer dưới không biết gì về layer trên.

---

## LA-01 — Dependency flow phải là một chiều (Core ← Service ← API)

```csharp
// ✅ Đúng: Service tham chiếu Core
// VegaBase.Service.csproj
<ProjectReference Include="..\VegaBase.Core\VegaBase.Core.csproj" />

// ❌ Sai: Core tham chiếu Service
// VegaBase.Core.csproj
<ProjectReference Include="..\VegaBase.Service\VegaBase.Service.csproj" />
```

---

## LA-02 — Core không được chứa EF Core attributes hoặc DbContext

Core là pure domain — không phụ thuộc vào bất kỳ infrastructure nào.

```csharp
// ✅ Đúng: POCO entity trong Core
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
}

// ❌ Sai: EF Core attributes trong Core
using Microsoft.EntityFrameworkCore;

[Table("Users")]
public class User : BaseEntity
{
    [Column("email")]
    public string Email { get; set; } = string.Empty;
}
```

---

## LA-03 — Service không được chứa HTTP types

Service layer không biết HTTP tồn tại. Không import `Microsoft.AspNetCore.*` trong Service.

```csharp
// ✅ Đúng: Service method nhận/trả domain types
public async Task<List<UserModel>> GetListAsync(UserParam param, ServiceMessage sMessage) { }

// ❌ Sai: Service nhận HttpContext hoặc trả IActionResult
public async Task<IActionResult> GetListAsync(HttpContext ctx, UserParam param) { }
```

---

## LA-04 — Business logic phải nằm trong Service, không được ở Controller

```csharp
// ✅ Đúng: validation trong CheckAddCondition (Service layer)
protected override async Task CheckAddCondition(User entity, UserParam param, ServiceMessage sMessage)
{
    var exists = await _db.QueryAsync<User>(q => q.Where(u => u.Email == param.Email));
    if (exists.IsSuccess && exists.Data.Any())
        sMessage += "Email đã tồn tại.";
}

// ❌ Sai: validation trong Controller
[HttpPost]
public async Task<IActionResult> Add([FromBody] UserParam param)
{
    if (!param.Email.Contains("@"))
        return BadRequest("Email không hợp lệ.");
    // ...
}
```

---

## LA-05 — API chỉ là HTTP wiring

Controller không chứa logic — chỉ gọi service, fill caller info, trả response.

```csharp
// ✅ Đúng: Controller chỉ wiring
[HttpPost]
public override async Task<IActionResult> Add([FromBody] UserParam param)
{
    FillCallerInfo(param);
    var sMessage = new ServiceMessage();
    var result = await _service.Add(param, sMessage);
    if (sMessage.HasError) return BadRequest(ApiResponse<UserModel>.Fail(sMessage.Value));
    return Ok(ApiResponse<UserModel>.Ok(result));
}

// ❌ Sai: Controller chứa business logic
[HttpPost]
public async Task<IActionResult> Add([FromBody] UserParam param)
{
    if (param.Age < 18) return BadRequest("Phải đủ 18 tuổi.");
    param.Email = param.Email.ToLower().Trim();
    // ... thêm logic khác
}
```

---

## LA-06 — Entity mới phải được định nghĩa trong Core

```csharp
// ✅ Đúng: Entity trong VegaBase.Core/Entities/
namespace VegaBase.Core.Entities;
public class Order : BaseEntity { }

// ❌ Sai: Entity trong Service hoặc API
namespace VegaBase.Service.Services;
public class Order : BaseEntity { }
```
````

- [ ] **Step 2: Commit**

```bash
git add docs/coding-standards/02-architecture.md
git commit -m "docs: add layer architecture rules (LA-01 to LA-06)"
```

---

## Task 4: Viết 03-base-classes.md

**Files:**
- Create: `docs/coding-standards/03-base-classes.md`

- [ ] **Step 1: Viết file**

Tạo file `docs/coding-standards/03-base-classes.md` với nội dung:

````markdown
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
// ✅ Đúng
protected override async Task CheckAddCondition(User entity, UserParam param, ServiceMessage sMessage)
{
    var result = await _db.QueryAsync<User>(q => q.Where(u => u.Email == entity.Email && !u.IsDeleted));
    if (result.IsSuccess && result.Data.Any())
        sMessage += "Email đã được sử dụng.";
}

// ❌ Sai: override Add() để validate
public override async Task Add(UserParam param, ServiceMessage sMessage)
{
    if (param.Email == "admin@example.com") { sMessage += "Email không hợp lệ."; return; }
    await base.Add(param, sMessage);
}
```

---

## BC-05 — Dùng `OnChanged` để invalidate cache sau write

```csharp
// ✅ Đúng
protected override async Task OnChanged(User entity, UserParam param)
{
    await _userCache.Invalidate(entity.Id);
}

// ❌ Sai: invalidate bên trong ApplyUpdate
protected override void ApplyUpdate(User entity, UserParam param)
{
    entity.Name = param.Name;
    _userCache.Invalidate(entity.Id).GetAwaiter().GetResult(); // sai chỗ, sai cách
}
```

---

## BC-06 — Dùng `param.HasField("FieldName")` cho partial update

```csharp
// ✅ Đúng: chỉ update field được gửi lên
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
// ✅ Đúng: batch load related data sau khi có danh sách
protected override async Task RefineListData(List<UserModel> models, UserParam param)
{
    var roleIds = models.Select(m => m.RoleId).Distinct().ToList();
    var roles = await _roleCache.GetAll();
    foreach (var m in models)
        m.RoleName = roles.FirstOrDefault(r => r.Id == m.RoleId)?.Name;
}

// ❌ Sai: query trong ApplyFilter gây N+1
protected override IQueryable<User> ApplyFilter(IQueryable<User> query, UserParam param)
{
    return query.Include(u => u.Role).Include(u => u.Department); // có thể dùng Include
    // nhưng không gọi query thêm trong vòng lặp bên trong filter
}
```
````

- [ ] **Step 2: Commit**

```bash
git add docs/coding-standards/03-base-classes.md
git commit -m "docs: add base classes rules (BC-01 to BC-10)"
```

---

## Task 5: Viết 04-error-handling.md

**Files:**
- Create: `docs/coding-standards/04-error-handling.md`

- [ ] **Step 1: Viết file**

Tạo file `docs/coding-standards/04-error-handling.md` với nội dung:

````markdown
# Error Handling

Quy tắc xử lý lỗi trong VegaBase — phân biệt rõ business error vs infrastructure error.

## Phân loại lỗi

| Loại | Xử lý bằng | Layer |
|---|---|---|
| Business validation | `ServiceMessage` | Service |
| DB operation result | `DbResult<T>` | Service / Infrastructure |
| Unexpected / infrastructure | `throw Exception` | Middleware bắt |
| HTTP response lỗi | `ApiResponse<T>.Fail()` | Controller |

---

## EH-01 — Dùng `ServiceMessage` cho business validation errors, không throw exception

```csharp
// ✅ Đúng
protected override async Task CheckAddCondition(User entity, UserParam param, ServiceMessage sMessage)
{
    if (string.IsNullOrEmpty(param.Email))
        sMessage += "Email không được để trống.";
}

// ❌ Sai
protected override async Task CheckAddCondition(User entity, UserParam param, ServiceMessage sMessage)
{
    if (string.IsNullOrEmpty(param.Email))
        throw new ArgumentException("Email không được để trống.");
}
```

---

## EH-02 — Tích lũy nhiều lỗi bằng `+=`, không return sớm sau lỗi đầu tiên

```csharp
// ✅ Đúng: thu thập tất cả lỗi
protected override async Task CheckAddCondition(User entity, UserParam param, ServiceMessage sMessage)
{
    if (string.IsNullOrEmpty(param.Email))
        sMessage += "Email không được để trống.";
    if (string.IsNullOrEmpty(param.Name))
        sMessage += "Tên không được để trống.";
    if (param.Age < 18)
        sMessage += "Phải đủ 18 tuổi.";
}

// ❌ Sai: return sau lỗi đầu tiên, mất các lỗi còn lại
protected override async Task CheckAddCondition(User entity, UserParam param, ServiceMessage sMessage)
{
    if (string.IsNullOrEmpty(param.Email)) { sMessage += "Email trống."; return; }
    if (string.IsNullOrEmpty(param.Name)) { sMessage += "Tên trống."; return; }
}
```

---

## EH-03 — Luôn kiểm tra `DbResult.IsSuccess` trước khi dùng `DbResult.Data`

```csharp
// ✅ Đúng
var result = await _db.AddAsync(entity);
if (!result.IsSuccess)
{
    sMessage += result.Error?.ToString() ?? "Lỗi khi thêm dữ liệu.";
    return;
}
var saved = result.Data;

// ❌ Sai: dùng Data mà không kiểm tra IsSuccess
var result = await _db.AddAsync(entity);
var saved = result.Data; // NullReferenceException nếu IsSuccess = false
```

---

## EH-04 — Không dùng exception để điều khiển business flow

```csharp
// ✅ Đúng: flow nghiệp vụ qua ServiceMessage
if (!hasPermission)
{
    sMessage += "Bạn không có quyền thực hiện thao tác này.";
    return;
}

// ❌ Sai: exception cho flow nghiệp vụ
if (!hasPermission)
    throw new UnauthorizedAccessException("Bạn không có quyền.");
```

---

## EH-05 — Không swallow exceptions (catch rỗng)

```csharp
// ✅ Đúng: log và re-throw hoặc xử lý có ý nghĩa
try
{
    await DoSomethingAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Lỗi khi thực hiện DoSomething");
    throw;
}

// ❌ Sai: catch rỗng
try
{
    await DoSomethingAsync();
}
catch (Exception) { }  // lỗi bị nuốt, không ai biết
```

---

## EH-06 — Không trả stack trace hoặc inner exception ra client

```csharp
// ✅ Đúng: message chung chung
return BadRequest(ApiResponse<object>.Fail("Đã xảy ra lỗi. Vui lòng thử lại."));

// ❌ Sai: lộ nội bộ
return BadRequest(ApiResponse<object>.Fail(ex.StackTrace));
return BadRequest(ApiResponse<object>.Fail(ex.InnerException?.Message));
```
````

- [ ] **Step 2: Commit**

```bash
git add docs/coding-standards/04-error-handling.md
git commit -m "docs: add error handling rules (EH-01 to EH-06)"
```

---

## Task 6: Viết 05-security.md

**Files:**
- Create: `docs/coding-standards/05-security.md`

- [ ] **Step 1: Viết file**

Tạo file `docs/coding-standards/05-security.md` với nội dung:

````markdown
# Security

Quy tắc bảo mật — password, JWT, authorization, và data exposure.

---

## SEC-01 — Luôn kiểm tra permission qua `IPermissionCache.HasPermission()`

Không tự kiểm tra role string bên trong service.

```csharp
// ✅ Đúng: kiểm tra qua permission cache
var allowed = await _permCache.HasPermission(param.CallerRoleIds, "UserManagement", "create");
if (!allowed) { sMessage += "Bạn không có quyền tạo người dùng."; return; }

// ❌ Sai: so sánh role thủ công
if (param.CallerRole != "admin") { sMessage += "Chỉ admin mới được tạo."; return; }
```

---

## SEC-02 — Tất cả controllers phải có `[Authorize]`

`BaseController` đã có `[Authorize]` — không override bằng `[AllowAnonymous]` trừ khi endpoint thực sự public và có comment giải thích.

```csharp
// ✅ Đúng: kế thừa [Authorize] từ BaseController (không cần làm gì thêm)
public class UserController : BaseController<IUserService, UserModel, UserParam> { }

// ✅ Cũng đúng nếu endpoint thực sự public (với comment)
[AllowAnonymous] // Public: dùng cho đăng nhập, không cần token
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginParam param) { }

// ❌ Sai: AllowAnonymous không có lý do
[AllowAnonymous]
public class UserController : BaseController<IUserService, UserModel, UserParam> { }
```

---

## SEC-03 — Chỉ dùng `IPasswordHasher` (Argon2id) để hash password

```csharp
// ✅ Đúng
var hashed = _hasher.Hash(param.Password);
var isValid = _hasher.Verify(param.Password, storedHash);

// ❌ Sai: thuật toán yếu hoặc không có salt
var hashed = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(param.Password)));
var hashed = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(param.Password)));
```

---

## SEC-04 — JWT secret phải đọc từ environment variable `JWT_SECRET`

```csharp
// ✅ Đúng
var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET chưa được cấu hình.");

// ❌ Sai: hardcode trong code hoặc appsettings
var secret = "my-super-secret-key-do-not-share";
var secret = _config["Jwt:Secret"]; // appsettings bị commit vào git
```

---

## SEC-05 — Không log password, token, hoặc thông tin nhạy cảm

```csharp
// ✅ Đúng: chỉ log thông tin định danh an toàn
_logger.LogInformation("User {Username} đăng nhập thành công", username);

// ❌ Sai
_logger.LogDebug("Thử mật khẩu: {Password}", param.Password);
_logger.LogInformation("Token: {Token}", jwtToken);
_logger.LogError("User data: {@User}", userWithPasswordHash);
```

---

## SEC-06 — Không trả password hash hoặc token trong `ApiResponse`

```csharp
// ✅ Đúng: UserModel không có PasswordHash
public class UserModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // PasswordHash KHÔNG có ở đây
}

// ❌ Sai: trả về thông tin nhạy cảm
return Ok(new { user.Email, user.PasswordHash, user.Log_CreatedBy });
```
````

- [ ] **Step 2: Commit**

```bash
git add docs/coding-standards/05-security.md
git commit -m "docs: add security rules (SEC-01 to SEC-06)"
```

---

## Task 7: Viết 06-database.md

**Files:**
- Create: `docs/coding-standards/06-database.md`

- [ ] **Step 1: Viết file**

Tạo file `docs/coding-standards/06-database.md` với nội dung:

````markdown
# Database

Quy tắc thao tác với database — soft delete, audit, transaction, và primary key.

---

## DB-01 — Luôn dùng `SoftDeleteAsync`, không xóa cứng

```csharp
// ✅ Đúng: logical delete
await _db.SoftDeleteAsync(entity, param.CallerUsername);

// ❌ Sai: physical delete
_context.Remove(entity);
await _context.SaveChangesAsync();
```

---

## DB-02 — Không set audit fields thủ công trong service

`UnitOfWork` và `DbActionExecutor` tự động set `Log_CreatedBy`, `Log_UpdatedDate`, v.v. trước khi commit.

```csharp
// ✅ Đúng: để infrastructure tự xử lý
await _uow.Add(entity);
await _uow.SaveAsync(param.CallerUsername);

// ❌ Sai: set tay trong service
entity.Log_CreatedBy = param.CallerUsername;
entity.Log_CreatedDate = DateTimeOffset.UtcNow;
await _uow.Add(entity);
await _uow.SaveAsync(param.CallerUsername);
```

---

## DB-03 — Multi-entity operations phải dùng `IUnitOfWork` + `SaveAsync()`

```csharp
// ✅ Đúng: atomic transaction
_uow.Add(order);
_uow.Add(orderItem);
_uow.Add(payment);
await _uow.SaveAsync(param.CallerUsername);

// ❌ Sai: 3 lần commit riêng biệt — không atomic
await _db.AddAsync(order);
await _db.AddAsync(orderItem);   // nếu lỗi ở đây, order đã được commit
await _db.AddAsync(payment);
```

---

## DB-04 — Single-entity operations dùng `IDbActionExecutor` trực tiếp

```csharp
// ✅ Đúng
var result = await _db.AddAsync(entity);
if (!result.IsSuccess) { sMessage += "Lỗi thêm dữ liệu."; return; }

// ❌ Sai: thao tác DbContext trực tiếp
_context.Add(entity);
await _context.SaveChangesAsync();
```

---

## DB-05 — Raw SQL phải có comment giải thích lý do

```csharp
// ✅ Đúng: raw SQL với lý do rõ ràng
// EF Core generates N individual UPDATE statements for batch; raw SQL is O(1)
await _context.Database.ExecuteSqlRawAsync(
    "UPDATE Products SET Stock = Stock - @qty WHERE Id = @id",
    new SqlParameter("@qty", quantity),
    new SqlParameter("@id", productId));

// ❌ Sai: raw SQL không có comment
await _context.Database.ExecuteSqlRawAsync(
    "DELETE FROM Logs WHERE CreatedDate < @cutoff",
    new SqlParameter("@cutoff", cutoff));
```

---

## DB-06 — Luôn filter `IsDeleted == false` trong `ApplyFilter`

```csharp
// ✅ Đúng
protected override IQueryable<User> ApplyFilter(IQueryable<User> query, UserParam param)
{
    query = query.Where(u => !u.IsDeleted);
    if (!string.IsNullOrEmpty(param.Keyword))
        query = query.Where(u => u.Name.Contains(param.Keyword));
    return query;
}

// ❌ Sai: quên filter IsDeleted — trả về cả record đã xóa
protected override IQueryable<User> ApplyFilter(IQueryable<User> query, UserParam param)
{
    return query.Where(u => u.Name.Contains(param.Keyword));
}
```

---

## DB-07 — Primary key là UUIDv7 tự sinh từ `BaseEntity`, không dùng int identity

```csharp
// ✅ Đúng: không gán Id (BaseEntity tự sinh UUIDv7)
var entity = new User { Name = param.Name, Email = param.Email };
await _db.AddAsync(entity);

// ❌ Sai: gán Id thủ công
var entity = new User { Id = Guid.NewGuid(), Name = param.Name }; // UUIDv4, không phải v7
var entity = new User { Id = someIntId }; // int identity không hỗ trợ
```
````

- [ ] **Step 2: Commit**

```bash
git add docs/coding-standards/06-database.md
git commit -m "docs: add database rules (DB-01 to DB-07)"
```

---

## Task 8: Viết 07-caching.md

**Files:**
- Create: `docs/coding-standards/07-caching.md`

- [ ] **Step 1: Viết file**

Tạo file `docs/coding-standards/07-caching.md` với nội dung:

````markdown
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
````

- [ ] **Step 2: Commit**

```bash
git add docs/coding-standards/07-caching.md
git commit -m "docs: add caching rules (CA-01 to CA-06)"
```

---

## Task 9: Viết 08-testing.md

**Files:**
- Create: `docs/coding-standards/08-testing.md`

- [ ] **Step 1: Viết file**

Tạo file `docs/coding-standards/08-testing.md` với nội dung:

````markdown
# Testing

Quy tắc viết test cho VegaBase — scope, structure, và naming.

## Phân loại test

| Loại | Scope | Tool |
|---|---|---|
| Unit test | Business logic hooks (`CheckAddCondition`, `ApplyFilter`) | xUnit |
| Integration test | `DbActionExecutor`, `UnitOfWork` với real DB | xUnit + EF Core InMemory hoặc Testcontainers |

---

## TS-01 — Unit test tập trung vào business logic hooks

```csharp
// ✅ Đúng: test CheckAddCondition với scenario cụ thể
[Fact]
public async Task CheckAddCondition_DuplicateEmail_AddsErrorToMessage()
{
    // Arrange
    var service = new UserService(mockDb.Object, mockPermCache.Object);
    var param = new UserParam { Email = "existing@example.com" };
    var sMessage = new ServiceMessage();
    mockDb.Setup(d => d.QueryAsync<User>(...)).ReturnsAsync(DbResult<List<User>>.Ok(existingUsers));

    // Act
    await service.InvokeCheckAddCondition(new User(), param, sMessage);

    // Assert
    Assert.True(sMessage.HasError);
}

// ❌ Sai: test GetList end-to-end trong unit test (integration concern)
[Fact]
public async Task GetList_ReturnsAllUsers()
{
    var result = await _service.GetList(new UserParam(), new ServiceMessage());
    Assert.NotEmpty(result); // cần DB thật để test này có ý nghĩa
}
```

---

## TS-02 — Integration test phải dùng real database, không mock `IDbActionExecutor`

```csharp
// ✅ Đúng: integration test với EF Core InMemory hoặc Testcontainers
public class UserDbTests : IAsyncLifetime
{
    private AppDbContext _context;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task AddAsync_NewUser_PersistsToDatabase()
    {
        var executor = new DbActionExecutor(_context, NullLogger<DbActionExecutor>.Instance);
        var user = new User { Name = "Test", Email = "test@example.com" };

        var result = await executor.AddAsync(user);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data.Id);
    }
}

// ❌ Sai: mock DbActionExecutor trong integration test
var mockDb = new Mock<IDbActionExecutor>();
mockDb.Setup(d => d.AddAsync(It.IsAny<User>()))
      .ReturnsAsync(DbResult<User>.Ok(new User()));
// → test này không kiểm tra được DB behavior thực sự
```

---

## TS-03 — Test method phải theo naming `MethodName_Scenario_ExpectedResult`

```csharp
// ✅ Đúng
[Fact] public async Task Add_DuplicateEmail_ReturnsError() { }
[Fact] public async Task GetList_WithKeyword_FiltersResults() { }
[Fact] public async Task Delete_NonExistentId_AddsErrorToMessage() { }
[Fact] public async Task UpdateField_EmailField_UpdatesOnlyEmail() { }

// ❌ Sai
[Fact] public async Task TestAdd() { }
[Fact] public async Task Test1() { }
[Fact] public async Task AddTest_Success() { }
[Fact] public async Task ShouldAddUser() { }
```

---

## TS-04 — Mỗi test có một assertion chính (AAA pattern)

```csharp
// ✅ Đúng: một assertion rõ ràng
[Fact]
public async Task Add_ValidParam_ReturnsSavedUser()
{
    // Arrange
    var param = new UserParam { Name = "Alice", Email = "alice@example.com" };
    var sMessage = new ServiceMessage();

    // Act
    var result = await _service.Add(param, sMessage);

    // Assert
    Assert.False(sMessage.HasError);
}

// ❌ Sai: nhiều assertion không liên quan trong một test
[Fact]
public async Task Add_ValidParam_EverythingWorks()
{
    var result = await _service.Add(param, sMessage);
    Assert.False(sMessage.HasError);
    Assert.NotNull(result);
    Assert.Equal(param.Email, result.Email);
    Assert.NotEqual(Guid.Empty, result.Id);
    Assert.True(result.Log_CreatedDate > DateTimeOffset.MinValue);
    // quá nhiều — nếu fail không biết cái nào gây ra vấn đề
}
```

---

## TS-05 — Test project phải nằm trong `VegaBase.[Layer].Tests/` tương ứng

```
// ✅ Đúng: cấu trúc tương ứng với project được test
VegaBase.Service.Tests/
    Services/
        UserServiceTests.cs
        ProductServiceTests.cs
    Infrastructure/
        DbActionExecutorTests.cs

VegaBase.API.Tests/
    Controllers/
        UserControllerTests.cs

// ❌ Sai: tất cả trong một file hoặc không có cấu trúc
Tests/
    AllTests.cs
```

---

## Lưu ý: Test project chưa tồn tại trong VegaBase

Khi tạo test project, thêm vào solution:

```bash
dotnet new xunit -n VegaBase.Service.Tests
dotnet sln add VegaBase.Service.Tests/VegaBase.Service.Tests.csproj
```

Thêm reference đến project cần test:

```xml
<!-- VegaBase.Service.Tests.csproj -->
<ItemGroup>
  <ProjectReference Include="..\VegaBase.Service\VegaBase.Service.csproj" />
</ItemGroup>
```
````

- [ ] **Step 2: Commit**

```bash
git add docs/coding-standards/08-testing.md
git commit -m "docs: add testing rules (TS-01 to TS-05)"
```

---

## Task 10: Commit design và plan docs

**Files:**
- Existing: `docs/superpowers/specs/2026-04-23-coding-standards-design.md`
- Existing: `docs/superpowers/plans/2026-04-23-coding-standards.md`

- [ ] **Step 1: Commit spec và plan**

```bash
git add docs/superpowers/specs/2026-04-23-coding-standards-design.md
git add docs/superpowers/plans/2026-04-23-coding-standards.md
git commit -m "docs: add coding standards design spec and implementation plan"
```

---

## Self-Review

**Spec coverage check:**
- ✅ NS-01..08: Task 2 (01-naming.md)
- ✅ LA-01..06: Task 3 (02-architecture.md)
- ✅ BC-01..10: Task 4 (03-base-classes.md)
- ✅ EH-01..06: Task 5 (04-error-handling.md)
- ✅ SEC-01..06: Task 6 (05-security.md)
- ✅ DB-01..07: Task 7 (06-database.md)
- ✅ CA-01..06: Task 8 (07-caching.md)
- ✅ TS-01..05: Task 9 (08-testing.md)
- ✅ README với rule index: Task 1

**Placeholder scan:** Không có TBD, TODO, hoặc "implement later".

**Type consistency:** Tất cả type references (`ServiceMessage`, `DbResult<T>`, `BaseParamModel`, `ICacheStore`) nhất quán với codebase hiện tại.
