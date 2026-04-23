# VegaBase Node.js Port — Design Spec

**Date:** 2026-04-23  
**Status:** Approved

---

## Goal

Port VegaBase (C# .NET 9 internal framework) to Node.js as a fully independent monorepo (`vegabase-node`), preserving the same architectural patterns — generic base service with hooks, RBAC permission cache, soft delete, audit trail — but using idiomatic TypeScript conventions instead of C#-style enterprise patterns.

---

## Section 1 — Tech Stack

| Layer | Technology |
|---|---|
| Language | TypeScript 5, Node.js 20+ |
| Web framework | Fastify |
| ORM | Prisma |
| Monorepo | pnpm workspaces |
| Validation | Zod |
| Password hashing | argon2 (npm) |
| JWT | @fastify/jwt |
| Testing | Vitest |

**Anti-patterns to avoid:**
- Generic types with 3+ type parameters
- Deep inheritance chains
- Over-abstraction (C#-style enterprise patterns)
- Complex DI containers (use Fastify plugins + decorators instead)

---

## Section 2 — Monorepo Structure

```
vegabase-node/
├── package.json              # root — workspaces, scripts
├── pnpm-workspace.yaml
├── tsconfig.base.json        # shared compiler options
└── packages/
    ├── core/
    │   ├── package.json      # name: @vegabase/core
    │   ├── tsconfig.json
    │   └── src/
    │       ├── common/
    │       │   ├── api-response.ts
    │       │   ├── result.ts
    │       │   └── service-error.ts
    │       ├── entities/
    │       │   └── base-entity.ts
    │       └── index.ts
    ├── service/
    │   ├── package.json      # name: @vegabase/service
    │   ├── tsconfig.json
    │   └── src/
    │       ├── base-service.ts
    │       ├── models/
    │       │   └── base-param-model.ts
    │       └── infrastructure/
    │           ├── db-actions/
    │           │   ├── db-result.ts
    │           │   ├── db-action-executor.ts
    │           │   └── unit-of-work.ts
    │           ├── cache/
    │           │   ├── cache-store.ts
    │           │   └── permission-cache.ts
    │           └── index.ts
    └── api/
        ├── package.json      # name: @vegabase/api
        ├── tsconfig.json
        └── src/
            ├── controllers/
            │   └── create-base-controller.ts
            ├── plugins/
            │   ├── jwt.ts
            │   ├── caller-info.ts
            │   └── error-handler.ts
            ├── password/
            │   ├── password-hasher.ts
            │   └── argon2id-hasher.ts
            └── index.ts
```

**Dependency flow:** `@vegabase/core ← @vegabase/service ← @vegabase/api` (one-way).

---

## Section 3 — @vegabase/core

### BaseEntity

```typescript
export interface BaseEntity {
  id: string;                     // UUIDv7
  isDeleted: boolean;
  logCreatedDate: Date;
  logCreatedBy: string;
  logUpdatedDate: Date | null;
  logUpdatedBy: string | null;
}
```

### Result<T> — unified return type (replaces dual ServiceMessage + return value)

```typescript
export type Result<T> =
  | { ok: true; data: T }
  | { ok: false; errors: ServiceError[] };
```

### ServiceError

```typescript
export interface ServiceError {
  code: string;       // e.g. "VALIDATION", "PERMISSION_DENIED", "NOT_FOUND"
  message: string;
  field?: string;     // for field-level validation errors
}
```

### Errors — internal accumulator used inside hooks

```typescript
export class Errors {
  private readonly list: ServiceError[] = [];
  add(code: string, message: string, field?: string): void {
    this.list.push({ code, message, field });
  }
  hasErrors(): boolean { return this.list.length > 0; }
  toResult<T>(): Result<T> { return { ok: false, errors: this.list }; }
}
```

### ApiResponse<T>

```typescript
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  errors?: ServiceError[];
  traceId: string;
}
```

### PagedResult<T>

```typescript
export interface PagedResult<T> {
  items: T[];
  total: number;
}
```

---

## Section 4 — @vegabase/service

### BaseParamModel

```typescript
export interface BaseParamModel {
  // Pagination
  page?: number;
  pageSize?: number;
  keyword?: string;
  sortBy?: string;
  sortDesc?: boolean;

  // Partial update tracking
  updatedFields?: string[];

  // Set by buildParam from JWT — never from client body
  callerUsername: string;
  callerRoles: string[];

  // Target record
  id?: string;
}
```

`hasField(field: string): boolean` — returns `true` when `updatedFields` is empty (update all) OR contains the field name. Mirrors C# `BaseParamModel.HasField`.

### DbResult<T>

```typescript
export type DbResult<T> =
  | { isSuccess: true; data: T; durationMs: number }
  | { isSuccess: false; error: DbError; durationMs: number };

export interface DbError {
  code: string;
  message: string;
  originalError?: unknown;
}
```

### DbActionExecutor

Wraps Prisma operations with retry and timeout:

```typescript
export interface DbActionOptions {
  retries?: number;       // default 2
  timeoutMs?: number;     // default 30_000
  retryDelayMs?: number;  // default 200
}

export class DbActionExecutor {
  constructor(
    private readonly prisma: PrismaClient,
    private readonly defaults: DbActionOptions = {}
  ) {}

  async addAsync<T>(delegate: PrismaDelegate<T>, data: Omit<T, 'id'>, createdBy: string): Promise<DbResult<T>>
  async updateAsync<T>(delegate: PrismaDelegate<T>, id: string, data: Partial<T>, updatedBy: string): Promise<DbResult<T>>
  async softDeleteAsync<T>(delegate: PrismaDelegate<T>, id: string, deletedBy: string): Promise<DbResult<boolean>>
  async queryAsync<T>(delegate: PrismaDelegate<T>, where: Record<string, unknown>): Promise<DbResult<T[]>>
  async getByIdAsync<T>(delegate: PrismaDelegate<T>, id: string): Promise<DbResult<T | null>>
}
```

Retry logic: exponential backoff on `P1008` (timeout) and connection errors. Never retry on `P2002` (unique constraint) or `P2025` (record not found).

### UnitOfWork

```typescript
export class UnitOfWork {
  constructor(private readonly prisma: PrismaClient) {}
  enqueue<T>(delegate: PrismaDelegate<T>, operation: PrismaOperation, createdBy?: string): void
  async saveAsync(): Promise<DbResult<void>>  // wraps prisma.$transaction
}
```

### BaseService<TModel, TParam>

2 generic type parameters (not 3).

```typescript
export abstract class BaseService<TModel extends BaseEntity, TParam extends BaseParamModel> {
  protected abstract readonly screenCode: string;
  protected abstract readonly delegate: PrismaDelegate<TModel>;
  protected abstract readonly allowedUpdateFields: ReadonlyArray<keyof TModel>;

  constructor(
    protected readonly executor: DbActionExecutor,
    protected readonly permissions: PermissionCache,
    protected readonly logger: Logger = noopLogger,
  ) {}

  // Public CRUD operations — all return Result<T>
  async getList(param: TParam): Promise<Result<PagedResult<TModel>>>
  async add(param: TParam): Promise<Result<TModel>>
  async updateField(param: TParam): Promise<Result<TModel>>
  async delete(param: TParam): Promise<Result<boolean>>

  // Override hooks — all optional
  protected applyFilter(where: Record<string, unknown>, param: TParam): Record<string, unknown>
  protected async checkAddCondition(param: TParam, errors: Errors): Promise<void>
  protected async checkUpdateCondition(param: TParam, errors: Errors): Promise<void>
  protected applyUpdate(entity: TModel, param: TParam): void  // uses allowedUpdateFields whitelist
  protected onChanged(): void
  protected async refineListData(items: TModel[], param: TParam, errors: Errors): Promise<void>
}
```

**`allowedUpdateFields` enforcement:** `applyUpdate` base implementation iterates only keys listed in `allowedUpdateFields`, skipping any other fields from `param`. Subclass calls `super.applyUpdate(entity, param)` or overrides entirely.

**`onChanged` pattern:** If a subclass needs the entity Id in `onChanged()`, store it in a private field during `applyUpdate`:

```typescript
private _lastChangedId = '';

protected applyUpdate(entity: UserModel, param: UserParam): void {
  this._lastChangedId = entity.id;
  super.applyUpdate(entity, param);
}

protected onChanged(): void {
  this.someCache.invalidate(this._lastChangedId);
}
```

### PermissionCache

TTL-based, no stale-read risk:

```typescript
export interface PermissionCacheOptions {
  ttlMs?: number;                        // default 300_000 (5 min)
  onInvalidate?: (roleId: string) => void;
}

export class PermissionCache {
  constructor(
    private readonly loader: (roleId: string) => Promise<string[]>,
    private readonly options: PermissionCacheOptions = {}
  ) {}

  async hasPermission(roleId: string, screenCode: string, action: string): Promise<boolean>
  invalidate(roleId: string): void
  invalidateAll(): void
}
```

`hasPermission` returns `Promise<boolean>` (async, because loader is async). Cache entries expire after `ttlMs`; expired entries are refetched on next access.

### CacheStore<TKey, TModel>

```typescript
export interface CacheStoreOptions {
  ttlMs?: number;
}

export class CacheStore<TKey, TModel> {
  constructor(private readonly options: CacheStoreOptions = {}) {}

  async getItem(key: TKey, loader: (key: TKey) => Promise<TModel | null>): Promise<TModel | null>
  async getAll(loader: () => Promise<TModel[]>): Promise<TModel[]>
  invalidate(key: TKey): void
  invalidateAll(): void
}
```

Loaders are async (unlike C# synchronous). TTL optional — omit for indefinite cache.

---

## Section 5 — @vegabase/api

### createBaseController

Factory function — no inheritance:

```typescript
export function createBaseController<TModel extends BaseEntity, TParam extends BaseParamModel>(options: {
  service: BaseService<TModel, TParam>;
  prefix: string;
  schemas: {
    list?: ZodSchema;
    add: ZodSchema;
    update: ZodSchema;
    delete?: ZodSchema;
  };
}): FastifyPluginAsync
```

Registers routes: `GET /list`, `POST /add`, `PUT /update-field`, `DELETE /delete`.

### buildParam — security boundary

```typescript
function buildParam<TParam extends BaseParamModel>(
  req: FastifyRequest,
  clientData: unknown,
  schema: ZodSchema<TParam>
): TParam {
  const parsed = schema.parse(clientData);
  // Overwrite any caller* fields with JWT-derived values — client cannot spoof
  return {
    ...parsed,
    callerUsername: req.caller.username,
    callerRoles: req.caller.roles,
  };
}
```

Caller identity always comes from `req.caller` (set by `callerInfoPlugin` from JWT), never from client body.

### HTTP status mapping

```typescript
const STATUS_MAP: Record<string, number> = {
  VALIDATION: 400,
  PERMISSION_DENIED: 403,
  NOT_FOUND: 404,
  DUPLICATE_KEY: 409,
  DB_TIMEOUT: 503,
  UNKNOWN: 500,
};

function errorsToStatus(errors: ServiceError[]): number {
  return STATUS_MAP[errors[0]?.code ?? 'UNKNOWN'] ?? 500;
}
```

### Plugin registration order

```typescript
// Required order in consumer app:
app.register(vegabaseJwtPlugin, { secret: process.env.JWT_SECRET });
app.register(callerInfoPlugin);  // depends on JWT
app.register(errorHandlerPlugin);
app.register(myController);      // depends on callerInfo
```

### errorHandlerPlugin

```typescript
app.setErrorHandler((error, req, reply) => {
  const traceId = crypto.randomUUID();
  logger.error({ traceId, error }, 'Unhandled error');
  reply.status(500).send({
    success: false,
    errors: [{ code: 'UNKNOWN', message: 'An unexpected error occurred.' }],
    traceId,
  });
});
```

Never expose stack traces or inner exception messages to clients.

### PasswordHasher

```typescript
export interface PasswordHasher {
  hash(password: string): Promise<string>;
  verify(password: string, hash: string): Promise<boolean>;
}

export interface Argon2Options {
  timeCost?: number;      // default 3
  memoryCost?: number;    // default 65536 (64 MB)
  parallelism?: number;   // default 4
  hashLength?: number;    // default 32
}

export class Argon2idHasher implements PasswordHasher {
  constructor(private readonly options: Argon2Options = {}) {}
}
```

### JWT plugin hardening

```typescript
// vegabaseJwtPlugin sets:
fastify.decorateRequest('caller', null);

// callerInfoPlugin populates req.caller from verified token:
req.caller = {
  username: payload.sub,
  roles: payload.roles ?? [],
};
Object.freeze(req.caller); // immutable — cannot be modified downstream
```

JWT verification errors → 401. Expired token → 401 with `code: "TOKEN_EXPIRED"`.

### Rate limiting (opt-in)

```typescript
// Consumer registers if needed:
app.register(import('@fastify/rate-limit'), {
  max: 100,
  timeWindow: '1 minute',
});
```

Not included by default — consumer decides.

---

## Section 6 — Error Handling

| Error type | Mechanism |
|---|---|
| Business validation | `Errors` accumulator in hooks → `Result<T>` |
| DB failures | `DbResult<T>` with `isSuccess: false` |
| Auth failures | 401 response from JWT plugin |
| Permission failures | `PERMISSION_DENIED` error code → 403 |
| Infrastructure / unexpected | throw → `errorHandlerPlugin` → 500 + traceId |

Never swallow exceptions. Never expose stack traces to clients. `traceId` on every error response links to server logs.

---

## Section 7 — Testing

| Type | Tool | Scope |
|---|---|---|
| Unit | Vitest | Hook logic (`checkAddCondition`, `applyFilter`) |
| Integration | Vitest + Prisma test DB | `DbActionExecutor`, `UnitOfWork` |
| Contract | Vitest | `createBaseController` route responses |

**Naming:** `methodName_scenario_expectedResult` (matches C# TS-03 rule).

**Pattern:** AAA (Arrange / Act / Assert), one assertion per test.

Integration tests use a real Prisma connection to a test database — never mock `DbActionExecutor` in integration tests.

---

## Decisions Log

| Decision | Rationale |
|---|---|
| 2 type params (`TModel`, `TParam`) | Avoid 3-param complexity; Entity type derived from delegate |
| `Result<T>` unified return | Eliminates dual-channel anti-pattern (ServiceMessage + return) |
| `allowedUpdateFields` abstract field | Compile-time whitelist enforcement prevents mass-assignment |
| Async `PermissionCache.hasPermission` | Loader is async; TTL prevents stale reads |
| Async `CacheStore` loaders | Natural in Node.js; no `getAwaiter().GetResult()` hacks |
| Factory function `createBaseController` | Avoids controller inheritance; plain Fastify plugin |
| `buildParam` strips caller fields | Security boundary — client cannot spoof identity |
| `Object.freeze(req.caller)` | Immutable identity throughout request lifecycle |
| Rate limiting opt-in | Consumer controls their own rate limits |
| Argon2 configurable options | Consumer tunes OWASP parameters per deployment |
