# Consumer Coding Standards

Bộ quy tắc dành cho developer **dùng VegaBase NuGet packages** để xây dựng ứng dụng.

## Audience

| Bạn là... | Đọc gì |
|---|---|
| Internal developer (contribute vào VegaBase) | Chỉ đọc `../01-08` |
| Consumer developer (dùng VegaBase NuGet) | Đọc `../01-08` (hiểu nguyên tắc) + `consumer/*` (cách áp dụng) |

## Mục lục

| # | File | Rule codes | Nội dung |
|---|---|---|---|
| 01 | [Project Setup](01-project-setup.md) | LA-07 → LA-12 | Project layout, DI wiring, middleware, env vars, startup sequence |
| 02 | [Entity & DbContext](02-entity-dbcontext.md) | NS-09, DB-08 → DB-13 | BaseEntity, AppDbContext, migrations, soft-delete |
| 03 | [Service & Controller](03-service-controller.md) | NS-10, BC-11 → BC-17 | BaseService, BaseController, hooks, Param |

## Tất cả Rule Codes

| Code | Tiêu đề | File |
|---|---|---|
| LA-07 | Project layout mirror VegaBase | [01-project-setup.md](01-project-setup.md) |
| LA-08 | DbContext bridge bắt buộc | [01-project-setup.md](01-project-setup.md) |
| LA-09 | DI đăng ký bắt buộc | [01-project-setup.md](01-project-setup.md) |
| LA-10 | Request buffering middleware | [01-project-setup.md](01-project-setup.md) |
| LA-11 | Env vars bắt buộc | [01-project-setup.md](01-project-setup.md) |
| LA-12 | Startup sequence cố định | [01-project-setup.md](01-project-setup.md) |
| NS-09 | Namespace entity theo project | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-08 | Inherit BaseEntity bắt buộc | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-09 | HasQueryFilter trên mọi entity | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-10 | Unique index phải kèm HasFilter | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-11 | Guid PK không auto-generate | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-12 | Override SaveChanges: soft-delete | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| DB-13 | Decimal phải khai báo precision | [02-entity-dbcontext.md](02-entity-dbcontext.md) |
| NS-10 | ScreenCode constant class | [03-service-controller.md](03-service-controller.md) |
| BC-11 | Inherit BaseService đúng generic | [03-service-controller.md](03-service-controller.md) |
| BC-12 | Override ScreenCode bắt buộc | [03-service-controller.md](03-service-controller.md) |
| BC-13 | Dùng hooks đúng mục đích | [03-service-controller.md](03-service-controller.md) |
| BC-14 | XParam phải chứa Data property | [03-service-controller.md](03-service-controller.md) |
| BC-15 | Controller inherit BaseController | [03-service-controller.md](03-service-controller.md) |
| BC-16 | Custom endpoint trả ApiResponse | [03-service-controller.md](03-service-controller.md) |
| BC-17 | Không inject DbContext vào Service | [03-service-controller.md](03-service-controller.md) |

## Cross-reference sang internal rules

Các consumer rules **mở rộng** internal rules — không lặp lại. Khi đọc consumer rule, tham chiếu về internal rule nền:

| Consumer rules | Mở rộng internal rules |
|---|---|
| LA-07 → LA-12 | [LA-01 → LA-06](../02-architecture.md) — dependency flow, layer responsibilities |
| DB-08 → DB-13 | [DB-01 → DB-07](../06-database.md) — soft delete, audit, transactions |
| BC-11 → BC-17 | [BC-01 → BC-10](../03-base-classes.md) — BaseService & BaseController patterns |
| NS-09, NS-10 | [NS-01 → NS-08](../01-naming.md) — naming conventions |
