# VegaBase

Framework .NET thư viện nội bộ cung cấp nền tảng chuẩn cho các dự án CRUD có phân quyền: entities, services, controllers, authentication, và caching.

## Tính năng chính

- **Base classes** cho CRUD (`BaseService`, `BaseController`, `BaseParamModel`) với các hook mở rộng — validate, filter, update, cache invalidation.
- **Phân quyền theo role + screen code** qua `IPermissionCache`.
- **Audit fields tự động** (`Log_CreatedDate`, `Log_CreatedBy`, …) và **soft delete** chuẩn hoá.
- **Authentication** JWT + password hashing Argon2id.
- **Dual DB provider** — PostgreSQL hoặc SQL Server, chuyển qua env var.
- **Read-through cache** cho master data (`ICacheStore<TKey, TModel>`).

## Tech Stack

| Thành phần | Phiên bản | Mục đích |
|---|---|---|
| .NET | 9.0 | Target framework |
| Entity Framework Core | 9.0.3 | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 | PostgreSQL provider |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.3 | SQL Server provider |
| Konscious.Security.Cryptography.Argon2 | 1.3.1 | Password hashing (Argon2id) |
| System.IdentityModel.Tokens.Jwt | 8.4.0 | JWT token generation/validation |

**Ngôn ngữ:** C# 12 (file-scoped namespaces, records, init-only properties). `ImplicitUsings` + `Nullable` enabled.

## Cấu trúc

```
vegabase/
├── VegaBase.Core/     ← Entities, DTOs, contracts (v1.0.3)
├── VegaBase.Service/  ← Business logic, infrastructure (v1.1.0)
├── VegaBase.API/      ← Controllers, middleware, JWT (v1.0.3)
└── docs/coding-standards/  ← Bộ quy tắc lập trình (54 internal + 21 consumer rules)
```

Dependency flow: `Core ← Service ← API` (một chiều). Mỗi project publish NuGet độc lập với version riêng.

## Bắt đầu

### Yêu cầu

- .NET SDK 9.0
- PostgreSQL hoặc SQL Server

### Cài đặt (consumer)

```bash
dotnet add package VegaBase.Core
dotnet add package VegaBase.Service
dotnet add package VegaBase.API
```

### Chạy solution (contributor)

```bash
dotnet build
dotnet run --project VegaBase.API
```

### Graph Codebase — AI tooling (contributor)

Repo tích hợp [code-review-graph](https://github.com/nicholaspretorius/code-review-graph) để cung cấp knowledge graph cho Claude Code. Claude sẽ hiểu quan hệ giữa các class/method và tránh viết code trùng lặp.

**Cài lần đầu (chỉ cần một lần):**

```bash
python -m pip install code-review-graph
python -m code_review_graph build --repo .
```

Sau đó **restart Claude Code** — MCP server tự kích hoạt nhờ `.mcp.json` đã có trong repo.

**Cập nhật graph** khi merge code mới về:

```bash
python -m code_review_graph build --repo .
```

> Hook `PostToolUse` trong `.claude/settings.json` tự cập nhật graph sau mỗi lần Claude sửa file, nên bình thường không cần chạy tay.

## Ví dụ sử dụng

Định nghĩa một entity + service + controller cho domain mới:

```csharp
// 1. Entity (VegaBase.Core)
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// 2. Model + Param (Param phải chứa Data để Add/UpdateField dùng)
public class ProductModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ProductParam : BaseParamModel
{
    public ProductModel? Data { get; set; }   // payload cho Add / UpdateField
    public string? SearchTerm { get; set; }   // filter cho GetList
}

// 3. Service — override abstract members + hook cần thiết
public class ProductService : BaseService<Product, ProductModel, ProductParam>
{
    public ProductService(
        IDbActionExecutor executor,
        IPermissionCache permissionCache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProductService> logger)
        : base(executor, permissionCache, httpContextAccessor, logger) { }

    protected override string ScreenCode => "Product";         // bắt buộc (abstract)
    protected override ProductModel GetAddData(ProductParam p) // bắt buộc (abstract)
        => p.Data!;

    protected override IQueryable<Product> ApplyFilter(
        IQueryable<Product> query, ProductParam p)
        => query.Where(x => !x.IsDeleted
            && (string.IsNullOrWhiteSpace(p.SearchTerm) || x.Name.Contains(p.SearchTerm)));

    protected override Task CheckAddCondition(ProductParam p, ServiceMessage msg)
    {
        if (p.Data is { Price: < 0 }) msg += "Giá phải >= 0";
        return Task.CompletedTask;
    }
}

// 4. Controller — thin wrapper
[Authorize]
[Route("api/[controller]")]
public class ProductController(ProductService service)
    : BaseController<ProductService, ProductModel, ProductParam>(service) { }
```

Chi tiết các hook và pattern:
- [docs/coding-standards/03-base-classes.md](docs/coding-standards/03-base-classes.md) — internal rules (BC-01 → BC-10)
- [docs/coding-standards/consumer/03-service-controller.md](docs/coding-standards/consumer/03-service-controller.md) — consumer rules (NS-10, BC-11 → BC-17)

## Environment Variables

| Biến | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|
| `JWT_SECRET` | Có | — | Khóa ký JWT — **không commit vào git** |
| `JWT_ISSUER` | Không | — | JWT issuer claim |
| `JWT_AUDIENCE` | Không | — | JWT audience claim |
| `JWT_EXPIRY_HOURS` | Không | 24 | Thời gian sống token (giờ) |
| `DB_IS_POSTGRESQL` | Không | `true` | `true` = PostgreSQL, `false` = SQL Server |
| `DB_HOST` | Không | `localhost` | |
| `DB_PORT` | Không | `5432` | |
| `DB_NAME` | Không | `AppDB` | |
| `DB_USER` | Không | `postgres` | |
| `DB_PASSWORD` | Không | — | |

## Tài liệu

- [Coding standards (internal)](docs/coding-standards/README.md) — 54 rules cho developer contribute vào VegaBase
- [Consumer coding standards](docs/coding-standards/consumer/README.md) — 21 rules cho developer dùng VegaBase NuGet
- [CLAUDE.md](CLAUDE.md) — hướng dẫn cho Claude Code khi làm việc trên repo
- [Design spec — internal standards](docs/superpowers/specs/2026-04-23-coding-standards-design.md)
- [Design spec — consumer standards](docs/superpowers/specs/2026-04-24-consumer-coding-standards-design.md)

## Publishing NuGet (maintainer)

Mỗi project version độc lập. Khi sửa project nào, bump version project đó:

```xml
<!-- ví dụ VegaBase.Service.csproj -->
<Version>1.0.6</Version>
```

**Quy tắc semver nội bộ:** `x.0.0` breaking · `1.x.0` feature · `1.0.x` fix/bump.

```bash
# Commit kèm version trong message
git commit -m "feat: <mô tả> (v1.0.6)"

# Pack + push lên feed nội bộ VegaLocal (C:\NuGet\)
dotnet pack VegaBase.Service -c Release -o C:\NuGet\

# Push lên nuget.org (khi public)
dotnet nuget push C:\NuGet\VegaBase.Service.1.0.6.nupkg \
  --api-key <API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

NuGet sources định nghĩa trong [nuget.config](nuget.config): `VegaLocal` (feed nội bộ `C:\NuGet\`) và `nuget.org`.
