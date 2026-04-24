# Security

Quy tắc bảo mật — password, JWT, authorization, và data exposure.

---

## SEC-01 — Không hardcode role string để kiểm tra quyền

`BaseService` tự động kiểm tra CRUD permission trước mỗi thao tác (View/Create/Edit/Delete). Trong các hook, dùng `CheckPermission()` cho custom permission check. `HasPermission()` là synchronous (không cần `await`).

> **Thứ tự kiểm tra trong `CheckPermission` (V1):** Admin bypass (`CallerRole == "admin"`) được kiểm tra **trước** `ScreenCode`. Admin luôn được phép qua kể cả khi `ScreenCode` chưa cấu hình. `ScreenCode` rỗng chỉ block non-admin.

```csharp
// ✅ Đúng: BaseService tự kiểm tra CRUD permission — không cần làm gì thêm cho Add/Edit/Delete/View
// ✅ Trong hook, dùng CheckPermission() nếu cần check thêm:
protected override async Task CheckAddCondition(UserParam param, ServiceMessage sMessage)
{
    CheckPermission(PermParam(param, "create"), sMessage); // dùng protected helper
    if (sMessage.HasError) return;
    // ... logic khác
}

// ✅ Nếu inject IPermissionCache riêng (ngoài BaseService context):
bool allowed = _permCache.HasPermission(param.CallerRoleIds, "UserManagement", "create"); // KHÔNG có await
if (!allowed) { sMessage += "Bạn không có quyền."; return; }

// ❌ Sai: so sánh role string thủ công — bypasses RBAC
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

> **Lưu ý khi xử lý danh sách roles (N1):** Tham số `roles` trong `GenerateToken` có thể là lazy `IEnumerable`. Không gọi `.Any()` sau `foreach` vì iterator đã bị exhausted — kết quả luôn `false`. Dùng flag boolean thay thế:
> ```csharp
> var hadAnyRoles = false;
> foreach (var (code, id) in roles)
> {
>     hadAnyRoles = true;
>     // ... xử lý
> }
> if (roleClaims == 0 && hadAnyRoles) _logger.LogError(...);
> ```

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
