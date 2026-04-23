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
