# VegaBase Coding Standards — Design Spec
**Date:** 2026-04-23  
**Status:** Approved  

---

## Mục tiêu

Tạo bộ quy tắc lập trình (coding standards) cho dự án VegaBase phục vụ 3 đối tượng:
1. **Developer mới** — onboarding nhanh, viết code đúng chuẩn ngay từ đầu
2. **Team nội bộ** — chuẩn hóa PR review, giảm tranh luận về style
3. **Người dùng thư viện** — tham chiếu khi tích hợp và extend VegaBase sau khi publish NuGet

---

## Quyết định thiết kế

| Thuộc tính | Quyết định |
|---|---|
| Cấu trúc | Multi-file có chỉ mục |
| Ngôn ngữ | Song ngữ (giải thích: tiếng Việt, code: tiếng Anh) |
| Ví dụ code | Snippet ngắn gọn với ✅ (đúng) và ❌ (sai) |
| Mã rule | Có prefix theo section (NS-01, LA-02...) để tham chiếu trong PR |

---

## Cấu trúc file

```
docs/
└── coding-standards/
    ├── README.md                 ← Chỉ mục + tổng quan + danh sách rule codes
    ├── 01-naming.md              ← Naming conventions
    ├── 02-architecture.md        ← Layer architecture rules
    ├── 03-base-classes.md        ← Cách extend BaseService & BaseController
    ├── 04-error-handling.md      ← ServiceMessage, DbResult, exceptions
    ├── 05-security.md            ← Password, JWT, RBAC
    ├── 06-database.md            ← Soft delete, audit, transactions
    ├── 07-caching.md             ← Cache rules
    └── 08-testing.md             ← Testing standards
```

---

## Nội dung từng file

### README.md — Chỉ mục
- Giới thiệu VegaBase và mục tiêu bộ quy tắc
- Bảng chỉ mục có link đến từng file
- Danh sách tất cả rule codes tổng hợp (NS-01 → TS-xx)
- Hướng dẫn cách tham chiếu rule trong PR comment

### 01-naming.md — Naming Conventions (prefix: NS)
- Namespace: `VegaBase.[Layer].[SubDomain]`
- Classes: PascalCase, suffix chuẩn (`Model`, `Param`, `Helper`, `Cache`)
- Interfaces: prefix `I` bắt buộc
- Audit fields: prefix `Log_` bắt buộc (`Log_CreatedDate`, `Log_UpdatedBy`)
- Screen codes: PascalCase, mô tả chức năng rõ ràng
- Enum values: PascalCase
- Async methods: suffix `Async`

### 02-architecture.md — Layer Architecture (prefix: LA)
- Dependency flow một chiều: Core ← Service ← API (không ngược lại)
- Core: chỉ chứa entities, DTOs, contracts — không có logic, không có EF
- Service: chứa business logic, infrastructure — không expose HTTP concerns
- API: chỉ HTTP wiring — không chứa business logic
- Không được tham chiếu namespace của layer trên từ layer dưới
- Không được import HttpContext hoặc ASP.NET types vào Service layer

### 03-base-classes.md — Base Classes (prefix: BC)
- Luôn kế thừa `BaseService<TEntity, TModel, TParam>` cho service CRUD
- Generic constraints phải đúng: `TEntity : BaseEntity`, `TModel : new()`, `TParam : BaseParamModel`
- Hook override theo từng mục đích:
  - `ApplyFilter()` — lọc query LINQ (không fetch rồi filter in-memory)
  - `CheckAddCondition()` — validate trước khi insert
  - `CheckUpdateCondition()` — validate trước khi update
  - `ApplyUpdate()` — map fields từ param sang entity (hoặc để `AutoApplyUpdate`)
  - `OnChanged()` — invalidate cache sau write
  - `RefineListData()` — enrich data sau khi load (join in-memory nếu cần)
- Partial update: dùng `param.HasField("FieldName")` trước khi gán giá trị
- Không override `GetList()`, `Add()`, `Delete()` trực tiếp — dùng hooks

### 04-error-handling.md — Error Handling (prefix: EH)
- `ServiceMessage` cho business validation errors (dùng `+=` để tích lũy)
- `DbResult<T>` cho database operation results — luôn kiểm tra `IsSuccess` trước khi dùng `Data`
- Chỉ throw exception cho lỗi unexpected/infrastructure (không dùng exception cho flow nghiệp vụ)
- Không swallow exceptions (catch rỗng hoặc catch + log-only mà không xử lý)
- `ApiResponse<T>.Fail()` chỉ dùng ở Controller layer khi `sMessage.HasError`
- Không trả về stack trace hoặc inner exception message ra client

### 05-security.md — Security (prefix: SEC)
- Luôn check permission qua `IPermissionCache.HasPermission()` trong service — không tự kiểm tra role string
- Không bypass `[Authorize]` attribute trên controller trừ endpoint public có lý do rõ ràng
- Password: chỉ dùng `IPasswordHasher` (Argon2id) — cấm MD5, SHA1, SHA256 thuần
- JWT: không hardcode secret trong code, đọc từ env var `JWT_SECRET`
- Không log password, token, hay thông tin nhạy cảm
- Không trả về password hash hay token trong `ApiResponse`

### 06-database.md — Database (prefix: DB)
- Luôn dùng soft delete (`SoftDeleteAsync`) — không dùng `DbContext.Remove()`
- Audit fields (`Log_CreatedBy`, `Log_UpdatedDate`...) được set bởi `UnitOfWork`/`DbActionExecutor` — không set tay trong service
- Multi-entity operations: dùng `IUnitOfWork` + `SaveAsync()` để đảm bảo transaction
- Single-entity operations: dùng `IDbActionExecutor` trực tiếp
- Không dùng raw SQL trừ performance-critical queries có comment giải thích
- Luôn filter `IsDeleted == false` trong `ApplyFilter()` (default từ BaseService)
- Primary key: UUIDv7 tự sinh từ `BaseEntity` — không dùng int identity

### 07-caching.md — Caching (prefix: CA)
- Chỉ cache dữ liệu ít thay đổi (lookup tables, permissions, config)
- Luôn dùng `ICacheStore<TKey, TCacheModel>` — không implement cache thủ công với Dictionary
- Luôn gọi `OnChanged()` sau write để invalidate cache liên quan
- Không cache dữ liệu người dùng nhạy cảm (password hash, token, PII)
- `PermissionCache` chỉ được invalidate khi role/permission thay đổi
- Không dùng cache để thay thế query — cache chỉ là optimization

### 08-testing.md — Testing (prefix: TS)
- Unit test: tập trung vào `CheckAddCondition`, `CheckUpdateCondition`, `ApplyFilter` logic
- Integration test: test `DbActionExecutor` với real database (không mock)
- Không mock `IDbActionExecutor` trong integration test — dùng test database
- Test naming: `MethodName_Scenario_ExpectedResult` (ví dụ: `Add_DuplicateEmail_ReturnsError`)
- Mỗi test: một assertion chính (AAA pattern: Arrange, Act, Assert)
- Test project: đặt trong `VegaBase.[Layer].Tests/`, tương ứng với project đang test

---

## Rule Code Index (tổng hợp)

| Prefix | Section | Số rules dự kiến |
|---|---|---|
| NS | Naming | ~8 |
| LA | Architecture | ~6 |
| BC | Base Classes | ~10 |
| EH | Error Handling | ~6 |
| SEC | Security | ~6 |
| DB | Database | ~7 |
| CA | Caching | ~6 |
| TS | Testing | ~5 |

---

## Không nằm trong scope

- Hướng dẫn setup môi trường dev
- CI/CD pipeline rules
- Git branching strategy
- API versioning strategy
