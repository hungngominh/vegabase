# VegaBase — Project Info

Framework .NET thư viện nội bộ cung cấp nền tảng chuẩn cho các dự án CRUD có phân quyền: entities, services, controllers, authentication, và caching.

---

## Tech Stack

| Thành phần | Phiên bản | Mục đích |
|---|---|---|
| .NET | 9.0 | Target framework |
| Entity Framework Core | 9.0.3 | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 | PostgreSQL provider |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.3 | SQL Server provider |
| Konscious.Security.Cryptography.Argon2 | 1.3.1 | Password hashing (Argon2id) |
| System.IdentityModel.Tokens.Jwt | 8.4.0 | JWT token generation/validation |

**Ngôn ngữ:** C# 12 (file-scoped namespaces, records, init-only properties).  
**Cấu hình chung:** `ImplicitUsings` + `Nullable` đều enabled.

---

## Cấu trúc project

```
vegabase/
├── vegabase.sln
├── nuget.config
├── VegaBase.Core/          ← Domain entities, DTOs, contracts (v1.0.3)
├── VegaBase.Service/       ← Business logic, infrastructure (v1.0.5)
├── VegaBase.API/           ← Controllers, middleware, JWT (v1.0.3)
└── docs/
    └── coding-standards/   ← Bộ quy tắc lập trình (54 rules)
```

**Dependency flow:** `Core ← Service ← API` (một chiều). Xem [docs/coding-standards/02-architecture.md](docs/coding-standards/02-architecture.md).

**NuGet packages publish riêng:** `VegaBase.Core`, `VegaBase.Service`, `VegaBase.API` — mỗi package có version độc lập.

---

## Build & Run

### Yêu cầu

- .NET SDK 9.0
- PostgreSQL hoặc SQL Server (tùy chọn qua env var)

### Các lệnh cơ bản

```bash
# Restore packages
dotnet restore

# Build toàn solution
dotnet build

# Build release
dotnet build -c Release

# Chạy API
dotnet run --project VegaBase.API
```

### Environment variables

| Biến | Mặc định | Mô tả |
|---|---|---|
| `DB_IS_POSTGRESQL` | `true` | `true` = PostgreSQL, `false` = SQL Server |
| `DB_HOST` | `localhost` | DB host |
| `DB_PORT` | `5432` (PG) | DB port |
| `DB_NAME` | `AppDB` | Tên database |
| `DB_USER` | `postgres` | DB user |
| `DB_PASSWORD` | (rỗng) | DB password |
| `JWT_SECRET` | — (bắt buộc) | Khóa ký JWT |
| `JWT_ISSUER` | — | JWT issuer claim |
| `JWT_AUDIENCE` | — | JWT audience claim |
| `JWT_EXPIRY_HOURS` | — | Thời gian sống token (giờ) |

> **Cảnh báo:** Không commit giá trị `JWT_SECRET` vào git. Xem [SEC-04](docs/coding-standards/05-security.md).

---

## NuGet sources

File [nuget.config](nuget.config) cấu hình 2 nguồn package:

| Nguồn | URL | Mục đích |
|---|---|---|
| `VegaLocal` | `C:\NuGet\` | Feed nội bộ cho các build nội bộ |
| `nuget.org` | `https://api.nuget.org/v3/index.json` | Public NuGet |

---

## Release process

Mỗi project trong solution có version độc lập. Khi một project thay đổi, bump chỉ version của project đó.

### 1. Bump version

Sửa tag `<Version>` trong `.csproj` tương ứng:

```xml
<!-- VegaBase.Service/VegaBase.Service.csproj -->
<Version>1.0.6</Version>
```

**Quy tắc semver nội bộ:**
- `x.0.0` — breaking change (API thay đổi không tương thích)
- `1.x.0` — tính năng mới, tương thích ngược
- `1.0.x` — bug fix, dependency bump

### 2. Commit với version trong message

```bash
git add VegaBase.Service/VegaBase.Service.csproj
git commit -m "feat: <mô tả> (v1.0.6)"
```

Tham chiếu các commit trước: `fdcf274` (v1.0.3), `b7f2714` (v1.0.2), `c21ddd3` (v1.0.1).

### 3. Pack và push lên feed nội bộ

```bash
# Pack một project (ví dụ Service)
dotnet pack VegaBase.Service/VegaBase.Service.csproj -c Release -o C:\NuGet\

# Hoặc pack toàn bộ
dotnet pack -c Release -o C:\NuGet\
```

Các file `.nupkg` sẽ xuất hiện trong `C:\NuGet\` và tự động available qua `VegaLocal` source.

### 4. Tag git (tùy chọn)

```bash
git tag service-v1.0.6
git push origin service-v1.0.6
```

### 5. Publish lên nuget.org (khi public)

```bash
dotnet nuget push C:\NuGet\VegaBase.Service.1.0.6.nupkg \
  --api-key <API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

---

## Tài liệu liên quan

- [Coding standards](docs/coding-standards/README.md) — 54 rules
- [Design spec](docs/superpowers/specs/2026-04-23-coding-standards-design.md)
- [Implementation plan](docs/superpowers/plans/2026-04-23-coding-standards.md)
