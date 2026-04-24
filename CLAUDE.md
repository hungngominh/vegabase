# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Coding Standards (MANDATORY)

All code — human or AI-generated — must follow these standards. **Read the relevant file before editing any `.cs` file in that area.** If proposed code would violate a rule, point it out and suggest the compliant approach instead of silently proceeding.

### Internal rules (contributing to VegaBase)

| File | Rules |
|------|-------|
| [docs/coding-standards/01-naming.md](docs/coding-standards/01-naming.md) | NS-01–NS-08: Namespaces, suffixes, audit field prefix, async naming |
| [docs/coding-standards/02-architecture.md](docs/coding-standards/02-architecture.md) | LA-01–LA-06: Layer dependency, Core purity, Service/API responsibilities |
| [docs/coding-standards/03-base-classes.md](docs/coding-standards/03-base-classes.md) | BC-01–BC-10: Which hooks to override, AutoApplyUpdate, RefineListData |
| [docs/coding-standards/04-error-handling.md](docs/coding-standards/04-error-handling.md) | EH-01–EH-06: ServiceMessage, DbResult, no silent catches |
| [docs/coding-standards/05-security.md](docs/coding-standards/05-security.md) | SEC-01–SEC-06: IPermissionCache, Argon2id, JWT from env vars |
| [docs/coding-standards/06-database.md](docs/coding-standards/06-database.md) | DB-01–DB-07: Soft delete, no manual audit fields, UUIDv7, UnitOfWork |
| [docs/coding-standards/07-caching.md](docs/coding-standards/07-caching.md) | CA-01–CA-06: ICacheStore usage, invalidation, what not to cache |
| [docs/coding-standards/08-testing.md](docs/coding-standards/08-testing.md) | TS-01–TS-05: xUnit, real DB, naming convention, AAA pattern |

### Consumer rules (applying VegaBase NuGet in downstream apps)

| File | Rules |
|------|-------|
| [docs/coding-standards/consumer/01-project-setup.md](docs/coding-standards/consumer/01-project-setup.md) | LA-07–LA-12: Project layout, DI wiring, middleware, env vars, startup sequence |
| [docs/coding-standards/consumer/02-entity-dbcontext.md](docs/coding-standards/consumer/02-entity-dbcontext.md) | NS-09, DB-08–DB-13: BaseEntity inheritance, HasQueryFilter, PK, soft-delete override |
| [docs/coding-standards/consumer/03-service-controller.md](docs/coding-standards/consumer/03-service-controller.md) | NS-10, BC-11–BC-17: ScreenCode constants, Service/Controller inheritance, Param.Data |

## Hard Rules (never break)

- **Never** set `Log_CreatedDate`, `Log_CreatedBy`, `Log_UpdatedDate`, `Log_UpdatedBy` manually — infrastructure sets these.
- **Never** physical delete — use `SoftDeleteAsync()`. Always filter `!entity.IsDeleted` in `ApplyFilter`.
- **Never** hardcode role strings — use `IPermissionCache.HasPermission(roleId, screenCode, action)`.
- **Never** cache passwords, tokens, or PII.
- **Never** catch exceptions just to re-throw or log-and-swallow. Don't expose stack traces in API responses.
- **Never** put business logic in API controllers, or ASP.NET types (`HttpContext`, etc.) in Service/Core.
- **Never** override core CRUD methods (`GetListAsync`, `AddAsync`, etc.) on `BaseService` — use the hooks.

## Coding Behavior

**Think before coding. State assumptions. Surface tradeoffs. Push back when warranted.**

- **Simplicity first** — no features beyond what was asked, no abstractions for single-use code, no speculative flexibility. If 200 lines could be 50, rewrite.
- **Surgical changes** — don't "improve" adjacent code, comments, or formatting. Match existing style. Only remove imports/variables your changes made unused.
- **Reuse before create** — before writing any new class, method, or utility, search the codebase for existing code that does the same or similar thing (use the Graph Codebase MCP tool if available). Extend or call what exists; only create new code when there is no suitable match. If you find a near-match, state the difference and ask before duplicating.
- **Goal-driven** — transform tasks into verifiable goals first. "Fix the bug" → reproduce with a test, then fix. For multi-step tasks, state a brief plan with a verify step per item before touching code.

## Project Architecture

.NET 9 library, three layers, strict unidirectional dependency:

```
VegaBase.Core    → Entities, DTOs, interfaces (no EF Core, no ASP.NET)
VegaBase.Service → Business logic (no HttpContext, no ASP.NET types)
VegaBase.API     → HTTP wiring only (controllers thin — delegate to Service)
```

## Key Base Classes (quick index)

### `BaseService<TEntity, TModel, TParam>`

Constructor requires 4 dependencies: `IDbActionExecutor`, `IPermissionCache`, `IHttpContextAccessor`, `ILogger<T>`.

**Abstract members — must override (or won't compile):**

| Member | Purpose |
|------|---------|
| `protected abstract string ScreenCode { get; }` | Screen code for `IPermissionCache.HasPermission` lookup. Empty string → all requests 403 with no error message. |
| `protected abstract TModel GetAddData(TParam param)` | Extracts the payload for `Add`. Consumer convention: `=> param.Data!` (see BC-14). |

**Virtual hooks — override when needed:**

| Hook | Purpose |
|------|---------|
| `ApplyFilter(IQueryable<T>, TParam)` | Synchronous LINQ filtering — **always override** (must at minimum filter `!IsDeleted`) |
| `Task CheckAddCondition(TParam, ServiceMessage)` | Async business validation before insert |
| `Task CheckUpdateCondition(TParam, ServiceMessage)` | Async business validation before update |
| `ApplyUpdate(TEntity, TParam)` | Custom mapping — default calls `AutoApplyUpdate()` which uses `HasField` + `param.Data` via reflection |
| `OnChanged()` | Synchronous cache invalidation, no params — exceptions caught & logged, write not rolled back |
| `Task RefineListData(List<TModel>, TParam, ServiceMessage)` | Post-load enrichment — avoid N+1 |
| `Task<List<TModel>> GetListCore(TParam, ServiceMessage)` | Override only when async cross-table filter is needed (don't `await` inside `ApplyFilter`) |

Details in [03-base-classes.md](docs/coding-standards/03-base-classes.md) (internal) and [consumer/03-service-controller.md](docs/coding-standards/consumer/03-service-controller.md) (how to apply in consumer apps).

### `BaseController<TService, TModel, TParam>`

Provides `GetList`, `GetItem`, `Add`, `UpdateField`, `Delete`. Call `FillCallerInfo()` at the top of any write action. All controllers require `[Authorize]`.

### `BaseParamModel`

All param models extend this. Key members: `CallerUsername`, `CallerRole`, `CallerRoleIds`, `Id`, `Page`/`PageSize` (defaults 1/20), `UpdatedFields` (`HashSet`). Use `HasField(fieldName)` in `ApplyUpdate` to check partial updates.

## Infrastructure Quick Reference

- **Single-entity writes:** `IDbActionExecutor`.
- **Multi-entity writes:** `ExecuteInTransactionAsync` with `IUnitOfWork`.
- **Primary keys:** UUIDv7 auto-generated by `BaseEntity`.
- **Validation errors:** accumulate via `ServiceMessage +=` (first error wins).
- **DB results:** check `DbResult<T>.IsSuccess` before `.Data`.
- **HTTP responses:** `ApiResponse<T>.Ok(data)` / `ApiResponse<T>.Fail(message)`.
- **Cache:** `ICacheStore<TKey, TCacheModel>` for read-heavy master data only (roles, permissions, categories). Invalidate from `OnChanged()`.

## Build & Run

```bash
dotnet build                     # debug build (runs restore implicitly)
dotnet build -c Release          # release build
dotnet run --project VegaBase.API
```

### Publishing NuGet Packages

Each project is versioned independently. Update `<Version>` in the `.csproj`, then:

```bash
dotnet pack VegaBase.<Layer> -c Release -o C:\NuGet\
dotnet nuget push C:\NuGet\VegaBase.<Layer>.<version>.nupkg --source VegaLocal
```

Commit message format: `feat: <description> (v1.0.x)`

## Environment Variables

| Variable | Required | Default |
|----------|----------|---------|
| `JWT_SECRET` | Yes | — |
| `JWT_ISSUER` | No | — |
| `JWT_AUDIENCE` | No | — |
| `JWT_EXPIRY_HOURS` | No | 24 |
| `DB_IS_POSTGRESQL` | No | `true` |
| `DB_HOST` | No | `localhost` |
| `DB_PORT` | No | `5432` |
| `DB_NAME` | No | `AppDB` |
| `DB_USER` | No | `postgres` |
| `DB_PASSWORD` | No | — |

## Contributor Workflow

If this repo's `origin` is `https://github.com/hungngominh/vegabase.git` and you are **not** `hungngominh`:

- Create a feature branch before editing: `git checkout -b <gh-username>/<short-desc>`
- After changes, push and open a PR targeting `main` via `gh pr create --base main`
- Never commit directly to `main` or `master`

Otherwise, follow the project's normal git flow.
