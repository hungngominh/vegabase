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
