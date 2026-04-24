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

**Shorthand với DI helper:** VegaBase cung cấp `AddVegaBaseDbContext<T>()` đọc `DB_*` env vars và tự chọn Npgsql/SQL Server + thêm bridge tự động:

```csharp
// Program.cs — thay thế cho đoạn trên
builder.Services.AddVegaBaseDbContext<AppDbContext>();
// Bridge được thêm tự động bên trong helper
```

---

## LA-09 — DI đăng ký bắt buộc

**Cách nhanh — dùng DI helpers:**

```csharp
// Program.cs
builder.Services.AddVegaBaseDbContext<AppDbContext>(); // LA-08
builder.Services.AddVegaBase();                        // đăng ký core + IJwtHelper
builder.Services.AddVegaBaseJwtAuthentication(builder.Configuration); // JWT bearer
```

`AddVegaBase()` đăng ký tất cả:

| Dependency | Lifetime |
|---|---|
| `IHttpContextAccessor` | (framework) |
| `IDbActionExecutor` | Scoped |
| `IPermissionCache` | Singleton |
| `IPasswordHasher` | Singleton |
| `IJwtHelper` | Singleton |

**Cách thủ công** (nếu không dùng helper):

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDbActionExecutor, DbActionExecutor>();
builder.Services.AddSingleton<IPermissionCache, PermissionCache>();
builder.Services.AddSingleton<IJwtHelper, JwtHelper>();
builder.Services.AddSingleton<IPasswordHasher, Argon2idHasher>();
```

Thiếu bất kỳ dependency nào → runtime crash khi resolve service đầu tiên của request.

> **Quan trọng:** `IPermissionCache` phải là **Singleton**. Đăng ký là Scoped hoặc Transient → cache rỗng mỗi request → mọi action non-admin trả 403.

---

## LA-10 — Request buffering middleware

`ExceptionHandlingMiddleware` (được cài qua `app.UseVegaBase()`) tự động gọi `EnableBuffering()` cho mọi request. Đây là lý do **phải gọi `UseVegaBase()` trước `UseAuthentication()`**:

```csharp
// Program.cs
var app = builder.Build();

app.UseVegaBase();          // cài ExceptionHandlingMiddleware + EnableBuffering tự động
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

**Không cần** thêm middleware `EnableBuffering` thủ công nữa — đã được xử lý bên trong `UseVegaBase()`.

> **Kỹ thuật:** `BaseController.UpdateField` đọc raw request body 2 lần — lần 1 để deserialize, lần 2 để detect partial-update fields qua `UpdatedFields`. Nếu body không được buffer, stream hết sau lần đọc đầu. VegaBase dùng `[EnableRequestBuffering]` attribute trên action này để đảm bảo, thêm `ExceptionHandlingMiddleware` gọi `EnableBuffering` là belt-and-suspenders.

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
await app.Services.GetRequiredService<IPermissionCache>().WarmAsync();
// warm other singleton caches here...

// 4. Middleware pipeline
app.UseVegaBase();          // ExceptionHandlingMiddleware + request buffering
app.UseCors();
app.UseHttpsRedirection();
// ...
app.Run();
```

**Lý do thứ tự:**
- Seed trước Migrate → bảng chưa tồn tại → exception
- Warm trước Seed → cache thiếu data mới seeded → stale cache ngay từ start
