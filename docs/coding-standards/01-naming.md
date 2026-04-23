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
