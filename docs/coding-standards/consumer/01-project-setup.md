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
