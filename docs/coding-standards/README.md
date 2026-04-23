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
