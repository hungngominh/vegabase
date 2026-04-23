# VegaBase Node.js Port — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create `e:/Work/vegabase-node/` monorepo with three npm packages (`@vegabase/core`, `@vegabase/service`, `@vegabase/api`) that port the VegaBase C# framework to TypeScript/Node.js.

**Architecture:** pnpm workspaces monorepo. One-way dependency: `core ← service ← api`. `BaseService<TModel, TParam>` provides hook-based CRUD returning `Result<T>`. `createBaseController` is a factory function (no inheritance) that generates Fastify routes. Caller identity always flows from JWT — never from client body.

**Tech Stack:** TypeScript 5, Node.js 20+, Fastify 5, Prisma 5, pnpm workspaces, Zod 3, argon2, @fastify/jwt, Vitest 2, uuid 9, fastify-plugin

**Spec:** `docs/superpowers/specs/2026-04-23-vegabase-node-design.md`

---

## File Map

```
e:/Work/vegabase-node/
├── package.json
├── pnpm-workspace.yaml
├── tsconfig.base.json
├── .gitignore
└── packages/
    ├── core/
    │   ├── package.json
    │   ├── tsconfig.json
    │   └── src/
    │       ├── common/service-error.ts
    │       ├── common/result.ts
    │       ├── common/errors.ts
    │       ├── common/api-response.ts
    │       ├── common/paged-result.ts
    │       ├── entities/base-entity.ts
    │       ├── __tests__/errors.test.ts
    │       └── index.ts
    ├── service/
    │   ├── package.json
    │   ├── tsconfig.json
    │   └── src/
    │       ├── models/base-param-model.ts
    │       ├── infrastructure/db-actions/db-result.ts
    │       ├── infrastructure/db-actions/prisma-delegate.ts
    │       ├── infrastructure/db-actions/db-action-executor.ts
    │       ├── infrastructure/db-actions/unit-of-work.ts
    │       ├── infrastructure/cache/cache-store.ts
    │       ├── infrastructure/cache/permission-cache.ts
    │       ├── base-service.ts
    │       ├── __tests__/base-param-model.test.ts
    │       ├── __tests__/db-result.test.ts
    │       ├── __tests__/db-action-executor.test.ts
    │       ├── __tests__/unit-of-work.test.ts
    │       ├── __tests__/cache-store.test.ts
    │       ├── __tests__/permission-cache.test.ts
    │       ├── __tests__/base-service.test.ts
    │       └── index.ts
    └── api/
        ├── package.json
        ├── tsconfig.json
        └── src/
            ├── plugins/jwt.ts
            ├── plugins/caller-info.ts
            ├── plugins/error-handler.ts
            ├── password/password-hasher.ts
            ├── password/argon2id-hasher.ts
            ├── controllers/create-base-controller.ts
            ├── __tests__/jwt.test.ts
            ├── __tests__/argon2id-hasher.test.ts
            ├── __tests__/create-base-controller.test.ts
            └── index.ts
```

---

### Task 1: Repo Scaffold

**Files:**
- Create: `e:/Work/vegabase-node/package.json`
- Create: `e:/Work/vegabase-node/pnpm-workspace.yaml`
- Create: `e:/Work/vegabase-node/tsconfig.base.json`
- Create: `e:/Work/vegabase-node/.gitignore`
- Create: `e:/Work/vegabase-node/packages/core/package.json`
- Create: `e:/Work/vegabase-node/packages/core/tsconfig.json`
- Create: `e:/Work/vegabase-node/packages/service/package.json`
- Create: `e:/Work/vegabase-node/packages/service/tsconfig.json`
- Create: `e:/Work/vegabase-node/packages/api/package.json`
- Create: `e:/Work/vegabase-node/packages/api/tsconfig.json`

- [ ] **Step 1: Create repo directory and init git**

```bash
mkdir e:/Work/vegabase-node
cd e:/Work/vegabase-node
git init
git checkout -b dev
```

- [ ] **Step 2: Create root config files**

`e:/Work/vegabase-node/.gitignore`:
```
node_modules/
dist/
*.tsbuildinfo
.env
```

`e:/Work/vegabase-node/pnpm-workspace.yaml`:
```yaml
packages:
  - 'packages/*'
```

`e:/Work/vegabase-node/package.json`:
```json
{
  "name": "vegabase-node",
  "private": true,
  "scripts": {
    "build": "pnpm -r build",
    "test": "pnpm -r test"
  },
  "engines": {
    "node": ">=20.0.0"
  }
}
```

`e:/Work/vegabase-node/tsconfig.base.json`:
```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "CommonJS",
    "lib": ["ES2022"],
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "declaration": true,
    "declarationMap": true,
    "sourceMap": true
  }
}
```

- [ ] **Step 3: Create package directories and config**

```bash
mkdir -p e:/Work/vegabase-node/packages/core/src/__tests__
mkdir -p e:/Work/vegabase-node/packages/service/src/__tests__
mkdir -p e:/Work/vegabase-node/packages/service/src/models
mkdir -p e:/Work/vegabase-node/packages/service/src/infrastructure/db-actions
mkdir -p e:/Work/vegabase-node/packages/service/src/infrastructure/cache
mkdir -p e:/Work/vegabase-node/packages/api/src/__tests__
mkdir -p e:/Work/vegabase-node/packages/api/src/plugins
mkdir -p e:/Work/vegabase-node/packages/api/src/password
mkdir -p e:/Work/vegabase-node/packages/api/src/controllers
```

`e:/Work/vegabase-node/packages/core/package.json`:
```json
{
  "name": "@vegabase/core",
  "version": "0.1.0",
  "main": "dist/index.js",
  "types": "dist/index.d.ts",
  "scripts": {
    "build": "tsc",
    "test": "vitest run"
  },
  "devDependencies": {
    "typescript": "^5.4.0",
    "vitest": "^2.0.0"
  }
}
```

`e:/Work/vegabase-node/packages/core/tsconfig.json`:
```json
{
  "extends": "../../tsconfig.base.json",
  "compilerOptions": {
    "outDir": "dist",
    "rootDir": "src"
  },
  "include": ["src"],
  "exclude": ["src/__tests__"]
}
```

`e:/Work/vegabase-node/packages/service/package.json`:
```json
{
  "name": "@vegabase/service",
  "version": "0.1.0",
  "main": "dist/index.js",
  "types": "dist/index.d.ts",
  "scripts": {
    "build": "tsc",
    "test": "vitest run"
  },
  "dependencies": {
    "@vegabase/core": "workspace:*",
    "@prisma/client": "^5.0.0",
    "uuid": "^9.0.0"
  },
  "devDependencies": {
    "@types/uuid": "^9.0.0",
    "typescript": "^5.4.0",
    "vitest": "^2.0.0"
  }
}
```

`e:/Work/vegabase-node/packages/service/tsconfig.json`:
```json
{
  "extends": "../../tsconfig.base.json",
  "compilerOptions": {
    "outDir": "dist",
    "rootDir": "src"
  },
  "include": ["src"],
  "exclude": ["src/__tests__"]
}
```

`e:/Work/vegabase-node/packages/api/package.json`:
```json
{
  "name": "@vegabase/api",
  "version": "0.1.0",
  "main": "dist/index.js",
  "types": "dist/index.d.ts",
  "scripts": {
    "build": "tsc",
    "test": "vitest run"
  },
  "dependencies": {
    "@vegabase/core": "workspace:*",
    "@vegabase/service": "workspace:*",
    "fastify": "^5.0.0",
    "@fastify/jwt": "^9.0.0",
    "fastify-plugin": "^5.0.0",
    "zod": "^3.22.0",
    "argon2": "^0.31.0"
  },
  "devDependencies": {
    "@types/node": "^20.0.0",
    "typescript": "^5.4.0",
    "vitest": "^2.0.0"
  }
}
```

`e:/Work/vegabase-node/packages/api/tsconfig.json`:
```json
{
  "extends": "../../tsconfig.base.json",
  "compilerOptions": {
    "outDir": "dist",
    "rootDir": "src"
  },
  "include": ["src"],
  "exclude": ["src/__tests__"]
}
```

- [ ] **Step 4: Install dependencies**

```bash
cd e:/Work/vegabase-node
pnpm install
```

Expected: dependencies installed, `node_modules` at root and in packages.

- [ ] **Step 5: Commit scaffold**

```bash
cd e:/Work/vegabase-node
git add .
git commit -m "chore: monorepo scaffold — pnpm workspace + tsconfig"
```

---

### Task 2: @vegabase/core

**Files:**
- Create: `packages/core/src/common/service-error.ts`
- Create: `packages/core/src/common/result.ts`
- Create: `packages/core/src/common/errors.ts`
- Create: `packages/core/src/common/api-response.ts`
- Create: `packages/core/src/common/paged-result.ts`
- Create: `packages/core/src/entities/base-entity.ts`
- Create: `packages/core/src/index.ts`
- Test: `packages/core/src/__tests__/errors.test.ts`

- [ ] **Step 1: Write the failing test**

`packages/core/src/__tests__/errors.test.ts`:
```typescript
import { describe, it, expect } from 'vitest';
import { Errors } from '../common/errors';

describe('Errors', () => {
  it('hasErrors_emptyList_returnsFalse', () => {
    const errors = new Errors();
    expect(errors.hasErrors()).toBe(false);
  });

  it('add_singleError_hasErrorsReturnsTrue', () => {
    const errors = new Errors();
    errors.add('VALIDATION', 'Email is required', 'email');
    expect(errors.hasErrors()).toBe(true);
  });

  it('add_multipleErrors_allStoredInOrder', () => {
    const errors = new Errors();
    errors.add('VALIDATION', 'Email required', 'email');
    errors.add('VALIDATION', 'Name required', 'name');
    expect(errors.all).toHaveLength(2);
    expect(errors.all[0].field).toBe('email');
    expect(errors.all[1].field).toBe('name');
  });

  it('toResult_withErrors_returnsFailResult', () => {
    const errors = new Errors();
    errors.add('VALIDATION', 'Email required', 'email');
    const result = errors.toResult<string>();
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errors).toHaveLength(1);
      expect(result.errors[0].code).toBe('VALIDATION');
      expect(result.errors[0].field).toBe('email');
    }
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd e:/Work/vegabase-node
pnpm --filter @vegabase/core test
```

Expected: FAIL — `Cannot find module '../common/errors'`

- [ ] **Step 3: Implement all core files**

`packages/core/src/common/service-error.ts`:
```typescript
export interface ServiceError {
  code: string;
  message: string;
  field?: string;
}
```

`packages/core/src/common/result.ts`:
```typescript
import type { ServiceError } from './service-error';

export type Result<T> =
  | { ok: true; data: T }
  | { ok: false; errors: ServiceError[] };

export function ok<T>(data: T): Result<T> {
  return { ok: true, data };
}

export function fail<T>(errors: ServiceError[]): Result<T> {
  return { ok: false, errors };
}
```

`packages/core/src/common/errors.ts`:
```typescript
import type { ServiceError } from './service-error';
import type { Result } from './result';
import { fail } from './result';

export class Errors {
  private readonly list: ServiceError[] = [];

  add(code: string, message: string, field?: string): void {
    this.list.push({ code, message, field });
  }

  hasErrors(): boolean {
    return this.list.length > 0;
  }

  toResult<T>(): Result<T> {
    return fail<T>([...this.list]);
  }

  get all(): ReadonlyArray<ServiceError> {
    return this.list;
  }
}
```

`packages/core/src/common/api-response.ts`:
```typescript
import type { ServiceError } from './service-error';

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  errors?: ServiceError[];
  traceId: string;
}

export function successResponse<T>(data: T, traceId: string): ApiResponse<T> {
  return { success: true, data, traceId };
}

export function failResponse<T>(errors: ServiceError[], traceId: string): ApiResponse<T> {
  return { success: false, errors, traceId };
}
```

`packages/core/src/common/paged-result.ts`:
```typescript
export interface PagedResult<T> {
  items: T[];
  total: number;
}
```

`packages/core/src/entities/base-entity.ts`:
```typescript
export interface BaseEntity {
  id: string;
  isDeleted: boolean;
  logCreatedDate: Date;
  logCreatedBy: string;
  logUpdatedDate: Date | null;
  logUpdatedBy: string | null;
}
```

`packages/core/src/index.ts`:
```typescript
export type { ServiceError } from './common/service-error';
export type { Result } from './common/result';
export { ok, fail } from './common/result';
export { Errors } from './common/errors';
export type { ApiResponse } from './common/api-response';
export { successResponse, failResponse } from './common/api-response';
export type { PagedResult } from './common/paged-result';
export type { BaseEntity } from './entities/base-entity';
```

- [ ] **Step 4: Run test to verify it passes**

```bash
pnpm --filter @vegabase/core test
```

Expected: PASS — 4 tests pass.

- [ ] **Step 5: Commit**

```bash
cd e:/Work/vegabase-node
git add packages/core/
git commit -m "feat(core): add BaseEntity, Result<T>, Errors, ApiResponse, PagedResult"
```

---

### Task 3: @vegabase/service — Base Types

**Files:**
- Create: `packages/service/src/models/base-param-model.ts`
- Create: `packages/service/src/infrastructure/db-actions/db-result.ts`
- Create: `packages/service/src/infrastructure/db-actions/prisma-delegate.ts`
- Test: `packages/service/src/__tests__/base-param-model.test.ts`
- Test: `packages/service/src/__tests__/db-result.test.ts`

- [ ] **Step 1: Write the failing tests**

`packages/service/src/__tests__/base-param-model.test.ts`:
```typescript
import { describe, it, expect } from 'vitest';
import { hasField } from '../models/base-param-model';
import type { BaseParamModel } from '../models/base-param-model';

describe('hasField', () => {
  const base: BaseParamModel = { callerUsername: 'user', callerRoles: ['ADMIN'] };

  it('hasField_noUpdatedFields_returnsTrue', () => {
    expect(hasField(base, 'name')).toBe(true);
  });

  it('hasField_emptyUpdatedFields_returnsTrue', () => {
    expect(hasField({ ...base, updatedFields: [] }, 'name')).toBe(true);
  });

  it('hasField_fieldInList_returnsTrue', () => {
    expect(hasField({ ...base, updatedFields: ['name', 'email'] }, 'name')).toBe(true);
  });

  it('hasField_fieldNotInList_returnsFalse', () => {
    expect(hasField({ ...base, updatedFields: ['email'] }, 'name')).toBe(false);
  });
});
```

`packages/service/src/__tests__/db-result.test.ts`:
```typescript
import { describe, it, expect } from 'vitest';
import { dbSuccess, dbFailure } from '../infrastructure/db-actions/db-result';

describe('DbResult', () => {
  it('dbSuccess_returnsSuccessResult', () => {
    const result = dbSuccess('data', 10);
    expect(result.isSuccess).toBe(true);
    if (result.isSuccess) {
      expect(result.data).toBe('data');
      expect(result.durationMs).toBe(10);
    }
  });

  it('dbFailure_returnsFailureResult', () => {
    const result = dbFailure({ code: 'P2002', message: 'Unique constraint' }, 5);
    expect(result.isSuccess).toBe(false);
    if (!result.isSuccess) {
      expect(result.error.code).toBe('P2002');
    }
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
pnpm --filter @vegabase/service test
```

Expected: FAIL — modules not found.

- [ ] **Step 3: Implement base types**

`packages/service/src/models/base-param-model.ts`:
```typescript
export interface BaseParamModel {
  page?: number;
  pageSize?: number;
  keyword?: string;
  sortBy?: string;
  sortDesc?: boolean;
  updatedFields?: string[];
  callerUsername: string;
  callerRoles: string[];
  id?: string;
}

export function hasField(param: BaseParamModel, field: string): boolean {
  if (!param.updatedFields || param.updatedFields.length === 0) return true;
  return param.updatedFields.includes(field);
}
```

`packages/service/src/infrastructure/db-actions/db-result.ts`:
```typescript
export interface DbError {
  code: string;
  message: string;
  originalError?: unknown;
}

export type DbResult<T> =
  | { isSuccess: true; data: T; durationMs: number }
  | { isSuccess: false; error: DbError; durationMs: number };

export function dbSuccess<T>(data: T, durationMs: number): DbResult<T> {
  return { isSuccess: true, data, durationMs };
}

export function dbFailure<T>(error: DbError, durationMs: number): DbResult<T> {
  return { isSuccess: false, error, durationMs };
}
```

`packages/service/src/infrastructure/db-actions/prisma-delegate.ts`:
```typescript
export interface PrismaDelegate<T> {
  create(args: { data: Record<string, unknown> }): Promise<T>;
  update(args: { where: { id: string }; data: Record<string, unknown> }): Promise<T>;
  findMany(args?: {
    where?: Record<string, unknown>;
    skip?: number;
    take?: number;
    orderBy?: Record<string, unknown>;
  }): Promise<T[]>;
  findUnique(args: { where: { id: string } }): Promise<T | null>;
  count(args?: { where?: Record<string, unknown> }): Promise<number>;
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
pnpm --filter @vegabase/service test
```

Expected: PASS — 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add packages/service/src/models/ packages/service/src/infrastructure/db-actions/db-result.ts packages/service/src/infrastructure/db-actions/prisma-delegate.ts packages/service/src/__tests__/base-param-model.test.ts packages/service/src/__tests__/db-result.test.ts
git commit -m "feat(service): add BaseParamModel, DbResult, PrismaDelegate"
```

---

### Task 4: @vegabase/service — DbActionExecutor

**Files:**
- Create: `packages/service/src/infrastructure/db-actions/db-action-executor.ts`
- Test: `packages/service/src/__tests__/db-action-executor.test.ts`

- [ ] **Step 1: Write the failing test**

`packages/service/src/__tests__/db-action-executor.test.ts`:
```typescript
import { describe, it, expect, vi } from 'vitest';
import { DbActionExecutor } from '../infrastructure/db-actions/db-action-executor';
import type { PrismaDelegate } from '../infrastructure/db-actions/prisma-delegate';

type TestEntity = { id: string; name: string; isDeleted: boolean; logCreatedDate: Date; logCreatedBy: string; logUpdatedDate: Date | null; logUpdatedBy: string | null };

function makeDelegate(overrides: Partial<PrismaDelegate<TestEntity>> = {}): PrismaDelegate<TestEntity> {
  return {
    create: vi.fn(),
    update: vi.fn(),
    findMany: vi.fn(),
    findUnique: vi.fn(),
    count: vi.fn(),
    ...overrides,
  };
}

describe('DbActionExecutor', () => {
  it('addAsync_success_returnsIsSuccessTrue', async () => {
    const entity: TestEntity = { id: 'abc', name: 'Test', isDeleted: false, logCreatedDate: new Date(), logCreatedBy: 'user', logUpdatedDate: null, logUpdatedBy: null };
    const delegate = makeDelegate({ create: vi.fn().mockResolvedValue(entity) });
    const executor = new DbActionExecutor();

    const result = await executor.addAsync(delegate, { name: 'Test' }, 'user');

    expect(result.isSuccess).toBe(true);
    if (result.isSuccess) expect(result.data).toBe(entity);
  });

  it('addAsync_delegateThrows_returnsIsSuccessFalse', async () => {
    const delegate = makeDelegate({ create: vi.fn().mockRejectedValue(new Error('db error')) });
    const executor = new DbActionExecutor();

    const result = await executor.addAsync(delegate, { name: 'Test' }, 'user');

    expect(result.isSuccess).toBe(false);
  });

  it('addAsync_timeout_returnsDbTimeoutCode', async () => {
    const delegate = makeDelegate({
      create: vi.fn().mockImplementation(() => new Promise(resolve => setTimeout(resolve, 500))),
    });
    const executor = new DbActionExecutor({ timeoutMs: 10, retries: 0 });

    const result = await executor.addAsync(delegate, { name: 'Test' }, 'user');

    expect(result.isSuccess).toBe(false);
    if (!result.isSuccess) expect(result.error.code).toBe('DB_TIMEOUT');
  });

  it('addAsync_setsAuditFields', async () => {
    let captured: Record<string, unknown> = {};
    const delegate = makeDelegate({
      create: vi.fn().mockImplementation(({ data }) => { captured = data; return Promise.resolve({ ...data }); }),
    });
    const executor = new DbActionExecutor();

    await executor.addAsync(delegate, { name: 'Alice' }, 'creator');

    expect(captured['logCreatedBy']).toBe('creator');
    expect(captured['isDeleted']).toBe(false);
    expect(captured['id']).toMatch(/^[0-9a-f-]{36}$/);
  });

  it('queryAsync_success_returnsItems', async () => {
    const items: TestEntity[] = [{ id: '1', name: 'A', isDeleted: false, logCreatedDate: new Date(), logCreatedBy: 'u', logUpdatedDate: null, logUpdatedBy: null }];
    const delegate = makeDelegate({ findMany: vi.fn().mockResolvedValue(items) });
    const executor = new DbActionExecutor();

    const result = await executor.queryAsync(delegate, { isDeleted: false });

    expect(result.isSuccess).toBe(true);
    if (result.isSuccess) expect(result.data).toHaveLength(1);
  });

  it('softDeleteAsync_success_returnsTrue', async () => {
    const entity: TestEntity = { id: '1', name: 'A', isDeleted: false, logCreatedDate: new Date(), logCreatedBy: 'u', logUpdatedDate: null, logUpdatedBy: null };
    const delegate = makeDelegate({ update: vi.fn().mockResolvedValue({ ...entity, isDeleted: true }) });
    const executor = new DbActionExecutor();

    const result = await executor.softDeleteAsync(delegate, '1', 'user');

    expect(result.isSuccess).toBe(true);
    if (result.isSuccess) expect(result.data).toBe(true);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

```bash
pnpm --filter @vegabase/service test
```

Expected: FAIL — `Cannot find module '../infrastructure/db-actions/db-action-executor'`

- [ ] **Step 3: Implement DbActionExecutor**

`packages/service/src/infrastructure/db-actions/db-action-executor.ts`:
```typescript
import { v7 as uuidv7 } from 'uuid';
import { dbSuccess, dbFailure, type DbResult, type DbError } from './db-result';
import type { PrismaDelegate } from './prisma-delegate';

const NON_RETRYABLE_CODES = new Set(['P2002', 'P2025', 'P2003']);

export interface DbActionOptions {
  retries?: number;
  timeoutMs?: number;
  retryDelayMs?: number;
}

export class DbActionExecutor {
  private readonly retries: number;
  private readonly timeoutMs: number;
  private readonly retryDelayMs: number;

  constructor(options: DbActionOptions = {}) {
    this.retries = options.retries ?? 2;
    this.timeoutMs = options.timeoutMs ?? 30_000;
    this.retryDelayMs = options.retryDelayMs ?? 200;
  }

  async addAsync<T>(
    delegate: PrismaDelegate<T>,
    data: Record<string, unknown>,
    createdBy: string,
  ): Promise<DbResult<T>> {
    return this.withRetry(() =>
      delegate.create({
        data: {
          ...data,
          id: uuidv7(),
          isDeleted: false,
          logCreatedDate: new Date(),
          logCreatedBy: createdBy,
          logUpdatedDate: null,
          logUpdatedBy: null,
        },
      }),
    );
  }

  async updateAsync<T>(
    delegate: PrismaDelegate<T>,
    id: string,
    data: Record<string, unknown>,
    updatedBy: string,
  ): Promise<DbResult<T>> {
    return this.withRetry(() =>
      delegate.update({
        where: { id },
        data: { ...data, logUpdatedDate: new Date(), logUpdatedBy: updatedBy },
      }),
    );
  }

  async softDeleteAsync<T>(
    delegate: PrismaDelegate<T>,
    id: string,
    deletedBy: string,
  ): Promise<DbResult<boolean>> {
    const result = await this.withRetry<T>(() =>
      delegate.update({
        where: { id },
        data: { isDeleted: true, logUpdatedDate: new Date(), logUpdatedBy: deletedBy },
      }),
    );
    if (!result.isSuccess) return { isSuccess: false, error: result.error, durationMs: result.durationMs };
    return { isSuccess: true, data: true, durationMs: result.durationMs };
  }

  async queryAsync<T>(
    delegate: PrismaDelegate<T>,
    where: Record<string, unknown>,
    options?: { skip?: number; take?: number; orderBy?: Record<string, unknown> },
  ): Promise<DbResult<T[]>> {
    return this.withRetry(() => delegate.findMany({ where, ...options }));
  }

  async getByIdAsync<T>(delegate: PrismaDelegate<T>, id: string): Promise<DbResult<T | null>> {
    return this.withRetry(() => delegate.findUnique({ where: { id } }));
  }

  async countAsync<T>(delegate: PrismaDelegate<T>, where: Record<string, unknown>): Promise<DbResult<number>> {
    return this.withRetry(() => delegate.count({ where }));
  }

  private async withRetry<T>(operation: () => Promise<T>): Promise<DbResult<T>> {
    let attempt = 0;
    while (true) {
      const start = Date.now();
      try {
        const data = await Promise.race([
          operation(),
          new Promise<never>((_, reject) =>
            setTimeout(() => reject(new Error('DB_TIMEOUT')), this.timeoutMs),
          ),
        ]);
        return dbSuccess(data, Date.now() - start);
      } catch (err) {
        const durationMs = Date.now() - start;
        if (this.isRetryable(err) && attempt < this.retries) {
          attempt++;
          await new Promise(resolve => setTimeout(resolve, this.retryDelayMs * attempt));
          continue;
        }
        return dbFailure(this.toDbError(err), durationMs);
      }
    }
  }

  private isRetryable(err: unknown): boolean {
    if (err instanceof Error && err.message === 'DB_TIMEOUT') return true;
    if (this.isPrismaError(err)) return !NON_RETRYABLE_CODES.has(err.code);
    return false;
  }

  private isPrismaError(err: unknown): err is { code: string; message: string } {
    return typeof err === 'object' && err !== null && 'code' in err && typeof (err as Record<string, unknown>).code === 'string';
  }

  private toDbError(err: unknown): DbError {
    if (err instanceof Error && err.message === 'DB_TIMEOUT') {
      return { code: 'DB_TIMEOUT', message: 'Database operation timed out.', originalError: err };
    }
    if (this.isPrismaError(err)) {
      return { code: err.code, message: err.message, originalError: err };
    }
    return { code: 'UNKNOWN', message: String(err), originalError: err };
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
pnpm --filter @vegabase/service test
```

Expected: PASS — all tests pass.

- [ ] **Step 5: Commit**

```bash
git add packages/service/src/infrastructure/db-actions/db-action-executor.ts packages/service/src/__tests__/db-action-executor.test.ts
git commit -m "feat(service): add DbActionExecutor with retry/timeout"
```

---

### Task 5: @vegabase/service — UnitOfWork

**Files:**
- Create: `packages/service/src/infrastructure/db-actions/unit-of-work.ts`
- Test: `packages/service/src/__tests__/unit-of-work.test.ts`

- [ ] **Step 1: Write the failing test**

`packages/service/src/__tests__/unit-of-work.test.ts`:
```typescript
import { describe, it, expect, vi } from 'vitest';
import { UnitOfWork } from '../infrastructure/db-actions/unit-of-work';

describe('UnitOfWork', () => {
  function makePrisma(transactionImpl?: (fn: (tx: unknown) => Promise<void>) => Promise<void>) {
    return {
      $transaction: vi.fn().mockImplementation(transactionImpl ?? ((fn: (tx: unknown) => Promise<void>) => fn({}))),
    } as unknown as import('@prisma/client').PrismaClient;
  }

  it('saveAsync_noOps_returnsSuccess', async () => {
    const uow = new UnitOfWork(makePrisma());
    const result = await uow.saveAsync();
    expect(result.isSuccess).toBe(true);
  });

  it('saveAsync_withEnqueuedOp_executesOp', async () => {
    const op = vi.fn().mockResolvedValue(undefined);
    const uow = new UnitOfWork(makePrisma());
    uow.enqueue(op);

    await uow.saveAsync();

    expect(op).toHaveBeenCalledOnce();
  });

  it('saveAsync_transactionFails_returnsFailure', async () => {
    const uow = new UnitOfWork(
      makePrisma(() => Promise.reject(new Error('constraint violation'))),
    );
    uow.enqueue(vi.fn());

    const result = await uow.saveAsync();

    expect(result.isSuccess).toBe(false);
    if (!result.isSuccess) expect(result.error.code).toBe('TRANSACTION_FAILED');
  });

  it('saveAsync_clearsQueueAfterSave', async () => {
    const op = vi.fn().mockResolvedValue(undefined);
    const uow = new UnitOfWork(makePrisma());
    uow.enqueue(op);
    await uow.saveAsync();

    await uow.saveAsync();

    expect(op).toHaveBeenCalledOnce(); // not twice
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

```bash
pnpm --filter @vegabase/service test
```

Expected: FAIL — `Cannot find module '../infrastructure/db-actions/unit-of-work'`

- [ ] **Step 3: Implement UnitOfWork**

`packages/service/src/infrastructure/db-actions/unit-of-work.ts`:
```typescript
import type { PrismaClient } from '@prisma/client';
import { dbSuccess, dbFailure, type DbResult } from './db-result';

export class UnitOfWork {
  private readonly ops: Array<(tx: PrismaClient) => Promise<unknown>> = [];

  constructor(private readonly prisma: PrismaClient) {}

  enqueue(op: (tx: PrismaClient) => Promise<unknown>): void {
    this.ops.push(op);
  }

  async saveAsync(): Promise<DbResult<void>> {
    const start = Date.now();
    const ops = [...this.ops];
    this.ops.length = 0;
    try {
      await this.prisma.$transaction(async tx => {
        for (const op of ops) {
          await op(tx as unknown as PrismaClient);
        }
      });
      return dbSuccess(undefined, Date.now() - start);
    } catch (err) {
      return dbFailure(
        { code: 'TRANSACTION_FAILED', message: String(err), originalError: err },
        Date.now() - start,
      );
    }
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
pnpm --filter @vegabase/service test
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add packages/service/src/infrastructure/db-actions/unit-of-work.ts packages/service/src/__tests__/unit-of-work.test.ts
git commit -m "feat(service): add UnitOfWork for atomic transactions"
```

---

### Task 6: @vegabase/service — CacheStore + PermissionCache

**Files:**
- Create: `packages/service/src/infrastructure/cache/cache-store.ts`
- Create: `packages/service/src/infrastructure/cache/permission-cache.ts`
- Test: `packages/service/src/__tests__/cache-store.test.ts`
- Test: `packages/service/src/__tests__/permission-cache.test.ts`

- [ ] **Step 1: Write the failing tests**

`packages/service/src/__tests__/cache-store.test.ts`:
```typescript
import { describe, it, expect, vi } from 'vitest';
import { CacheStore } from '../infrastructure/cache/cache-store';

describe('CacheStore', () => {
  it('getItem_cacheMiss_callsLoader', async () => {
    const store = new CacheStore<string, string>();
    const loader = vi.fn().mockResolvedValue('value');

    const result = await store.getItem('key', loader);

    expect(loader).toHaveBeenCalledWith('key');
    expect(result).toBe('value');
  });

  it('getItem_cacheHit_doesNotCallLoader', async () => {
    const store = new CacheStore<string, string>();
    const loader = vi.fn().mockResolvedValue('value');
    await store.getItem('key', loader);

    await store.getItem('key', loader);

    expect(loader).toHaveBeenCalledOnce();
  });

  it('invalidate_removesEntry_loaderCalledAgain', async () => {
    const store = new CacheStore<string, string>();
    const loader = vi.fn().mockResolvedValue('value');
    await store.getItem('key', loader);
    store.invalidate('key');

    await store.getItem('key', loader);

    expect(loader).toHaveBeenCalledTimes(2);
  });

  it('getAll_cacheMiss_callsLoader', async () => {
    const store = new CacheStore<string, string>();
    const loader = vi.fn().mockResolvedValue(['a', 'b']);

    const result = await store.getAll(loader);

    expect(result).toEqual(['a', 'b']);
    expect(loader).toHaveBeenCalledOnce();
  });

  it('getAll_cacheHit_doesNotCallLoader', async () => {
    const store = new CacheStore<string, string>();
    const loader = vi.fn().mockResolvedValue(['a']);
    await store.getAll(loader);

    await store.getAll(loader);

    expect(loader).toHaveBeenCalledOnce();
  });

  it('invalidateAll_clearsItemAndAllCache', async () => {
    const store = new CacheStore<string, string>();
    const itemLoader = vi.fn().mockResolvedValue('v');
    const allLoader = vi.fn().mockResolvedValue(['v']);
    await store.getItem('k', itemLoader);
    await store.getAll(allLoader);
    store.invalidateAll();

    await store.getItem('k', itemLoader);
    await store.getAll(allLoader);

    expect(itemLoader).toHaveBeenCalledTimes(2);
    expect(allLoader).toHaveBeenCalledTimes(2);
  });
});
```

`packages/service/src/__tests__/permission-cache.test.ts`:
```typescript
import { describe, it, expect, vi } from 'vitest';
import { PermissionCache } from '../infrastructure/cache/permission-cache';

describe('PermissionCache', () => {
  it('hasPermission_roleHasPermission_returnsTrue', async () => {
    const loader = vi.fn().mockResolvedValue(['USERS:READ', 'USERS:CREATE']);
    const cache = new PermissionCache(loader);

    const result = await cache.hasPermission('admin', 'USERS', 'READ');

    expect(result).toBe(true);
  });

  it('hasPermission_roleMissingPermission_returnsFalse', async () => {
    const loader = vi.fn().mockResolvedValue(['USERS:READ']);
    const cache = new PermissionCache(loader);

    const result = await cache.hasPermission('viewer', 'USERS', 'DELETE');

    expect(result).toBe(false);
  });

  it('hasPermission_calledTwice_loaderCalledOnce', async () => {
    const loader = vi.fn().mockResolvedValue(['USERS:READ']);
    const cache = new PermissionCache(loader);
    await cache.hasPermission('admin', 'USERS', 'READ');

    await cache.hasPermission('admin', 'USERS', 'READ');

    expect(loader).toHaveBeenCalledOnce();
  });

  it('invalidate_clearsRole_loaderCalledAgain', async () => {
    const loader = vi.fn().mockResolvedValue(['USERS:READ']);
    const cache = new PermissionCache(loader);
    await cache.hasPermission('admin', 'USERS', 'READ');
    cache.invalidate('admin');

    await cache.hasPermission('admin', 'USERS', 'READ');

    expect(loader).toHaveBeenCalledTimes(2);
  });

  it('invalidate_callsOnInvalidateCallback', async () => {
    const onInvalidate = vi.fn();
    const cache = new PermissionCache(async () => [], { onInvalidate });

    cache.invalidate('role-1');

    expect(onInvalidate).toHaveBeenCalledWith('role-1');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
pnpm --filter @vegabase/service test
```

Expected: FAIL — modules not found.

- [ ] **Step 3: Implement CacheStore and PermissionCache**

`packages/service/src/infrastructure/cache/cache-store.ts`:
```typescript
interface CacheEntry<T> {
  data: T;
  expiresAt: number | null;
}

export class CacheStore<TKey, TModel> {
  private readonly cache = new Map<TKey, CacheEntry<TModel>>();
  private allEntry: CacheEntry<TModel[]> | null = null;
  private readonly ttlMs: number | null;

  constructor(options: { ttlMs?: number } = {}) {
    this.ttlMs = options.ttlMs ?? null;
  }

  async getItem(key: TKey, loader: (key: TKey) => Promise<TModel | null>): Promise<TModel | null> {
    const entry = this.cache.get(key);
    if (entry && (entry.expiresAt === null || Date.now() < entry.expiresAt)) {
      return entry.data;
    }
    const data = await loader(key);
    if (data !== null) {
      this.cache.set(key, { data, expiresAt: this.ttlMs ? Date.now() + this.ttlMs : null });
    }
    return data;
  }

  async getAll(loader: () => Promise<TModel[]>): Promise<TModel[]> {
    if (this.allEntry && (this.allEntry.expiresAt === null || Date.now() < this.allEntry.expiresAt)) {
      return this.allEntry.data;
    }
    const data = await loader();
    this.allEntry = { data, expiresAt: this.ttlMs ? Date.now() + this.ttlMs : null };
    return data;
  }

  invalidate(key: TKey): void {
    this.cache.delete(key);
  }

  invalidateAll(): void {
    this.cache.clear();
    this.allEntry = null;
  }
}
```

`packages/service/src/infrastructure/cache/permission-cache.ts`:
```typescript
interface CacheEntry {
  permissions: Set<string>;
  expiresAt: number;
}

export interface PermissionCacheOptions {
  ttlMs?: number;
  onInvalidate?: (roleId: string) => void;
}

export class PermissionCache {
  private readonly cache = new Map<string, CacheEntry>();
  private readonly ttlMs: number;
  private readonly onInvalidate?: (roleId: string) => void;

  constructor(
    private readonly loader: (roleId: string) => Promise<string[]>,
    options: PermissionCacheOptions = {},
  ) {
    this.ttlMs = options.ttlMs ?? 300_000;
    this.onInvalidate = options.onInvalidate;
  }

  async hasPermission(roleId: string, screenCode: string, action: string): Promise<boolean> {
    const perms = await this.getPermissions(roleId);
    return perms.has(`${screenCode}:${action}`);
  }

  invalidate(roleId: string): void {
    this.cache.delete(roleId);
    this.onInvalidate?.(roleId);
  }

  invalidateAll(): void {
    this.cache.clear();
  }

  private async getPermissions(roleId: string): Promise<Set<string>> {
    const entry = this.cache.get(roleId);
    if (entry && Date.now() < entry.expiresAt) {
      return entry.permissions;
    }
    const perms = await this.loader(roleId);
    const set = new Set(perms);
    this.cache.set(roleId, { permissions: set, expiresAt: Date.now() + this.ttlMs });
    return set;
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
pnpm --filter @vegabase/service test
```

Expected: PASS — all tests pass.

- [ ] **Step 5: Commit**

```bash
git add packages/service/src/infrastructure/cache/ packages/service/src/__tests__/cache-store.test.ts packages/service/src/__tests__/permission-cache.test.ts
git commit -m "feat(service): add CacheStore and PermissionCache with TTL"
```

---

### Task 7: @vegabase/service — BaseService

**Files:**
- Create: `packages/service/src/base-service.ts`
- Create: `packages/service/src/index.ts`
- Test: `packages/service/src/__tests__/base-service.test.ts`

- [ ] **Step 1: Write the failing test**

`packages/service/src/__tests__/base-service.test.ts`:
```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { BaseService } from '../base-service';
import type { BaseParamModel } from '../models/base-param-model';
import type { DbActionExecutor } from '../infrastructure/db-actions/db-action-executor';
import type { PermissionCache } from '../infrastructure/cache/permission-cache';
import type { PrismaDelegate } from '../infrastructure/db-actions/prisma-delegate';
import { dbSuccess, dbFailure } from '../infrastructure/db-actions/db-result';
import type { BaseEntity } from '@vegabase/core';
import { Errors } from '@vegabase/core';

interface UserEntity extends BaseEntity {
  name: string;
  email: string;
}

interface UserParam extends BaseParamModel {
  name?: string;
  email?: string;
}

class TestUserService extends BaseService<UserEntity, UserParam> {
  protected readonly screenCode = 'USERS';
  protected readonly delegate: PrismaDelegate<UserEntity>;
  protected readonly allowedUpdateFields = ['name', 'email'] as const satisfies ReadonlyArray<keyof UserEntity>;

  constructor(executor: DbActionExecutor, permissions: PermissionCache) {
    super(executor, permissions);
    this.delegate = {
      create: vi.fn(),
      update: vi.fn(),
      findMany: vi.fn(),
      findUnique: vi.fn(),
      count: vi.fn(),
    };
  }

  protected buildNewEntity(param: UserParam): Record<string, unknown> {
    return { name: param.name ?? '', email: param.email ?? '' };
  }
}

function makeExecutor(): DbActionExecutor {
  return {
    addAsync: vi.fn(),
    updateAsync: vi.fn(),
    softDeleteAsync: vi.fn(),
    queryAsync: vi.fn(),
    getByIdAsync: vi.fn(),
    countAsync: vi.fn(),
  } as unknown as DbActionExecutor;
}

function makePermissions(hasPermission = true): PermissionCache {
  return { hasPermission: vi.fn().mockResolvedValue(hasPermission), invalidate: vi.fn(), invalidateAll: vi.fn() } as unknown as PermissionCache;
}

function makeParam(overrides: Partial<UserParam> = {}): UserParam {
  return { callerUsername: 'user', callerRoles: ['ADMIN'], ...overrides };
}

function makeEntity(overrides: Partial<UserEntity> = {}): UserEntity {
  return { id: 'eid', name: 'Alice', email: 'a@b.com', isDeleted: false, logCreatedDate: new Date(), logCreatedBy: 'user', logUpdatedDate: null, logUpdatedBy: null, ...overrides };
}

describe('BaseService.add', () => {
  it('add_noPermission_returnsPermissionDenied', async () => {
    const svc = new TestUserService(makeExecutor(), makePermissions(false));
    const result = await svc.add(makeParam());
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.errors[0].code).toBe('PERMISSION_DENIED');
  });

  it('add_validParam_callsExecutorAddAsync', async () => {
    const executor = makeExecutor();
    const entity = makeEntity();
    vi.mocked(executor.addAsync).mockResolvedValue(dbSuccess(entity, 5));
    const svc = new TestUserService(executor, makePermissions());

    const result = await svc.add(makeParam({ name: 'Alice', email: 'a@b.com' }));

    expect(result.ok).toBe(true);
    expect(executor.addAsync).toHaveBeenCalledOnce();
  });

  it('add_dbFailure_returnsError', async () => {
    const executor = makeExecutor();
    vi.mocked(executor.addAsync).mockResolvedValue(dbFailure({ code: 'P1001', message: 'conn refused' }, 5));
    const svc = new TestUserService(executor, makePermissions());

    const result = await svc.add(makeParam({ name: 'Alice' }));

    expect(result.ok).toBe(false);
  });

  it('checkAddCondition_addsError_addReturnsEarly', async () => {
    class StrictService extends TestUserService {
      protected override async checkAddCondition(param: UserParam, errors: Errors): Promise<void> {
        errors.add('VALIDATION', 'Name required', 'name');
      }
    }
    const executor = makeExecutor();
    const svc = new StrictService(executor, makePermissions());

    const result = await svc.add(makeParam());

    expect(result.ok).toBe(false);
    expect(executor.addAsync).not.toHaveBeenCalled();
  });
});

describe('BaseService.delete', () => {
  it('delete_noId_returnsValidationError', async () => {
    const svc = new TestUserService(makeExecutor(), makePermissions());
    const result = await svc.delete(makeParam());
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.errors[0].code).toBe('VALIDATION');
  });

  it('delete_entityNotFound_returnsNotFound', async () => {
    const executor = makeExecutor();
    vi.mocked(executor.getByIdAsync).mockResolvedValue(dbSuccess(null, 5));
    const svc = new TestUserService(executor, makePermissions());

    const result = await svc.delete(makeParam({ id: 'missing' }));

    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.errors[0].code).toBe('NOT_FOUND');
  });

  it('delete_exists_callsSoftDelete', async () => {
    const executor = makeExecutor();
    vi.mocked(executor.getByIdAsync).mockResolvedValue(dbSuccess(makeEntity(), 5));
    vi.mocked(executor.softDeleteAsync).mockResolvedValue(dbSuccess(true, 5));
    const svc = new TestUserService(executor, makePermissions());

    const result = await svc.delete(makeParam({ id: 'eid' }));

    expect(result.ok).toBe(true);
    expect(executor.softDeleteAsync).toHaveBeenCalledOnce();
  });
});

describe('BaseService.getList', () => {
  it('getList_success_returnsPagedResult', async () => {
    const executor = makeExecutor();
    const entity = makeEntity();
    vi.mocked(executor.queryAsync).mockResolvedValue(dbSuccess([entity], 5));
    vi.mocked(executor.countAsync).mockResolvedValue(dbSuccess(1, 5));
    const svc = new TestUserService(executor, makePermissions());

    const result = await svc.getList(makeParam());

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.items).toHaveLength(1);
      expect(result.data.total).toBe(1);
    }
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

```bash
pnpm --filter @vegabase/service test
```

Expected: FAIL — `Cannot find module '../base-service'`

- [ ] **Step 3: Implement BaseService**

`packages/service/src/base-service.ts`:
```typescript
import { ok, fail, Errors, type Result, type BaseEntity, type PagedResult } from '@vegabase/core';
import { hasField, type BaseParamModel } from './models/base-param-model';
import type { DbActionExecutor } from './infrastructure/db-actions/db-action-executor';
import type { PermissionCache } from './infrastructure/cache/permission-cache';
import type { PrismaDelegate } from './infrastructure/db-actions/prisma-delegate';

export interface Logger {
  info(msg: string, ...args: unknown[]): void;
  error(msg: string, ...args: unknown[]): void;
}

export const noopLogger: Logger = { info: () => {}, error: () => {} };

export abstract class BaseService<TModel extends BaseEntity, TParam extends BaseParamModel> {
  protected abstract readonly screenCode: string;
  protected abstract readonly delegate: PrismaDelegate<TModel>;
  protected abstract readonly allowedUpdateFields: ReadonlyArray<keyof TModel>;

  constructor(
    protected readonly executor: DbActionExecutor,
    protected readonly permissions: PermissionCache,
    protected readonly logger: Logger = noopLogger,
  ) {}

  async getList(param: TParam): Promise<Result<PagedResult<TModel>>> {
    const allowed = await this.permissions.hasPermission(param.callerRoles[0] ?? '', this.screenCode, 'READ');
    if (!allowed) return fail([{ code: 'PERMISSION_DENIED', message: 'Access denied.' }]);

    const where = this.applyFilter({}, param);
    const page = param.page ?? 1;
    const pageSize = param.pageSize ?? 20;

    const [itemsResult, countResult] = await Promise.all([
      this.executor.queryAsync(this.delegate, where, {
        skip: (page - 1) * pageSize,
        take: pageSize,
        orderBy: param.sortBy ? { [param.sortBy]: param.sortDesc ? 'desc' : 'asc' } : undefined,
      }),
      this.executor.countAsync(this.delegate, where),
    ]);

    if (!itemsResult.isSuccess) return fail([{ code: 'DB_TIMEOUT', message: itemsResult.error.message }]);
    if (!countResult.isSuccess) return fail([{ code: 'DB_TIMEOUT', message: countResult.error.message }]);

    const errors = new Errors();
    await this.refineListData(itemsResult.data, param, errors);
    if (errors.hasErrors()) return errors.toResult();

    return ok({ items: itemsResult.data, total: countResult.data });
  }

  async add(param: TParam): Promise<Result<TModel>> {
    const allowed = await this.permissions.hasPermission(param.callerRoles[0] ?? '', this.screenCode, 'CREATE');
    if (!allowed) return fail([{ code: 'PERMISSION_DENIED', message: 'Access denied.' }]);

    const errors = new Errors();
    await this.checkAddCondition(param, errors);
    if (errors.hasErrors()) return errors.toResult();

    const data = this.buildNewEntity(param);
    const result = await this.executor.addAsync(this.delegate, data, param.callerUsername);
    if (!result.isSuccess) {
      if (result.error.code === 'P2002') return fail([{ code: 'DUPLICATE_KEY', message: 'Record already exists.' }]);
      return fail([{ code: 'DB_TIMEOUT', message: result.error.message }]);
    }

    this.onChanged();
    return ok(result.data);
  }

  async updateField(param: TParam): Promise<Result<TModel>> {
    if (!param.id) return fail([{ code: 'VALIDATION', message: 'id is required.', field: 'id' }]);

    const allowed = await this.permissions.hasPermission(param.callerRoles[0] ?? '', this.screenCode, 'UPDATE');
    if (!allowed) return fail([{ code: 'PERMISSION_DENIED', message: 'Access denied.' }]);

    const entityResult = await this.executor.getByIdAsync(this.delegate, param.id);
    if (!entityResult.isSuccess) return fail([{ code: 'DB_TIMEOUT', message: entityResult.error.message }]);
    if (!entityResult.data) return fail([{ code: 'NOT_FOUND', message: 'Record not found.' }]);

    const errors = new Errors();
    await this.checkUpdateCondition(param, errors);
    if (errors.hasErrors()) return errors.toResult();

    const entity = entityResult.data;
    this.applyUpdate(entity, param);

    const data: Record<string, unknown> = {};
    for (const field of this.allowedUpdateFields) {
      if (hasField(param, String(field))) {
        data[String(field)] = (entity as unknown as Record<string, unknown>)[String(field)];
      }
    }

    const result = await this.executor.updateAsync(this.delegate, param.id, data, param.callerUsername);
    if (!result.isSuccess) return fail([{ code: 'DB_TIMEOUT', message: result.error.message }]);

    this.onChanged();
    return ok(result.data);
  }

  async delete(param: TParam): Promise<Result<boolean>> {
    if (!param.id) return fail([{ code: 'VALIDATION', message: 'id is required.', field: 'id' }]);

    const allowed = await this.permissions.hasPermission(param.callerRoles[0] ?? '', this.screenCode, 'DELETE');
    if (!allowed) return fail([{ code: 'PERMISSION_DENIED', message: 'Access denied.' }]);

    const entityResult = await this.executor.getByIdAsync(this.delegate, param.id);
    if (!entityResult.isSuccess) return fail([{ code: 'DB_TIMEOUT', message: entityResult.error.message }]);
    if (!entityResult.data) return fail([{ code: 'NOT_FOUND', message: 'Record not found.' }]);

    const result = await this.executor.softDeleteAsync(this.delegate, param.id, param.callerUsername);
    if (!result.isSuccess) return fail([{ code: 'DB_TIMEOUT', message: result.error.message }]);

    this.onChanged();
    return ok(true);
  }

  protected applyFilter(where: Record<string, unknown>, _param: TParam): Record<string, unknown> {
    return { ...where, isDeleted: false };
  }

  protected async checkAddCondition(_param: TParam, _errors: Errors): Promise<void> {}
  protected async checkUpdateCondition(_param: TParam, _errors: Errors): Promise<void> {}

  protected applyUpdate(entity: TModel, param: TParam): void {
    for (const field of this.allowedUpdateFields) {
      const key = String(field);
      if (hasField(param, key) && key in (param as unknown as Record<string, unknown>)) {
        (entity as unknown as Record<string, unknown>)[key] = (param as unknown as Record<string, unknown>)[key];
      }
    }
  }

  protected onChanged(): void {}

  protected async refineListData(_items: TModel[], _param: TParam, _errors: Errors): Promise<void> {}

  protected abstract buildNewEntity(param: TParam): Record<string, unknown>;
}
```

- [ ] **Step 4: Create service package index**

`packages/service/src/index.ts`:
```typescript
export { BaseService, noopLogger, type Logger } from './base-service';
export { hasField, type BaseParamModel } from './models/base-param-model';
export { dbSuccess, dbFailure, type DbResult, type DbError } from './infrastructure/db-actions/db-result';
export type { PrismaDelegate } from './infrastructure/db-actions/prisma-delegate';
export { DbActionExecutor, type DbActionOptions } from './infrastructure/db-actions/db-action-executor';
export { UnitOfWork } from './infrastructure/db-actions/unit-of-work';
export { CacheStore } from './infrastructure/cache/cache-store';
export { PermissionCache, type PermissionCacheOptions } from './infrastructure/cache/permission-cache';
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
pnpm --filter @vegabase/service test
```

Expected: PASS — all tests pass.

- [ ] **Step 6: Commit**

```bash
git add packages/service/src/base-service.ts packages/service/src/index.ts packages/service/src/__tests__/base-service.test.ts
git commit -m "feat(service): add BaseService with hooks, Result<T>, permission checks"
```

---

### Task 8: @vegabase/api — Plugins

**Files:**
- Create: `packages/api/src/plugins/jwt.ts`
- Create: `packages/api/src/plugins/caller-info.ts`
- Create: `packages/api/src/plugins/error-handler.ts`
- Test: `packages/api/src/__tests__/jwt.test.ts`

- [ ] **Step 1: Write the failing test**

`packages/api/src/__tests__/jwt.test.ts`:
```typescript
import { describe, it, expect } from 'vitest';
import Fastify from 'fastify';
import { vegabaseJwtPlugin } from '../plugins/jwt';

describe('vegabaseJwtPlugin', () => {
  it('request_withoutToken_returns401', async () => {
    const app = Fastify();
    await app.register(vegabaseJwtPlugin, { secret: 'test-secret-key-long-enough' });
    app.get('/test', async () => ({ ok: true }));
    await app.ready();

    const res = await app.inject({ method: 'GET', url: '/test' });

    expect(res.statusCode).toBe(401);
  });

  it('request_withValidToken_returns200', async () => {
    const app = Fastify();
    await app.register(vegabaseJwtPlugin, { secret: 'test-secret-key-long-enough' });
    app.get('/test', async () => ({ ok: true }));
    await app.ready();

    const token = app.jwt.sign({ sub: 'user1', roles: ['ADMIN'] });
    const res = await app.inject({ method: 'GET', url: '/test', headers: { authorization: `Bearer ${token}` } });

    expect(res.statusCode).toBe(200);
  });

  it('request_withExpiredToken_returns401', async () => {
    const app = Fastify();
    await app.register(vegabaseJwtPlugin, { secret: 'test-secret-key-long-enough' });
    app.get('/test', async () => ({ ok: true }));
    await app.ready();

    // expiresIn in seconds — 1 is minimum, test the code path not timing
    const token = app.jwt.sign({ sub: 'user1' }, { expiresIn: '-1s' });
    const res = await app.inject({ method: 'GET', url: '/test', headers: { authorization: `Bearer ${token}` } });

    expect(res.statusCode).toBe(401);
    const body = res.json<{ errors: { code: string }[] }>();
    expect(body.errors[0].code).toBe('UNAUTHORIZED');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

```bash
pnpm --filter @vegabase/api test
```

Expected: FAIL — `Cannot find module '../plugins/jwt'`

- [ ] **Step 3: Implement plugins**

`packages/api/src/plugins/jwt.ts`:
```typescript
import type { FastifyInstance } from 'fastify';
import fp from 'fastify-plugin';
import jwtPlugin from '@fastify/jwt';

export interface JwtConfig {
  secret: string;
  issuer?: string;
  audience?: string;
}

export const vegabaseJwtPlugin = fp(async (app: FastifyInstance, config: JwtConfig) => {
  await app.register(jwtPlugin, {
    secret: config.secret,
    verify: { issuer: config.issuer, audience: config.audience },
  });

  app.addHook('onRequest', async (req, reply) => {
    try {
      await req.jwtVerify();
    } catch {
      return reply.status(401).send({
        success: false,
        errors: [{ code: 'UNAUTHORIZED', message: 'Invalid or missing token.' }],
        traceId: crypto.randomUUID(),
      });
    }
  });
});
```

`packages/api/src/plugins/caller-info.ts`:
```typescript
import type { FastifyInstance } from 'fastify';
import fp from 'fastify-plugin';

export interface CallerInfo {
  username: string;
  roles: string[];
}

declare module 'fastify' {
  interface FastifyRequest {
    caller: CallerInfo;
  }
}

export const callerInfoPlugin = fp(async (app: FastifyInstance) => {
  app.decorateRequest('caller', null);

  app.addHook('preHandler', async req => {
    const payload = req.user as { sub?: string; roles?: string[] };
    const caller: CallerInfo = Object.freeze({
      username: payload?.sub ?? 'unknown',
      roles: payload?.roles ?? [],
    });
    req.caller = caller;
  });
});
```

`packages/api/src/plugins/error-handler.ts`:
```typescript
import type { FastifyInstance } from 'fastify';
import fp from 'fastify-plugin';

export const errorHandlerPlugin = fp(async (app: FastifyInstance) => {
  app.setErrorHandler((error, _req, reply) => {
    const traceId = crypto.randomUUID();
    app.log.error({ traceId, error }, 'Unhandled error');
    reply.status(500).send({
      success: false,
      errors: [{ code: 'UNKNOWN', message: 'An unexpected error occurred.' }],
      traceId,
    });
  });
});
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
pnpm --filter @vegabase/api test
```

Expected: PASS — 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/plugins/ packages/api/src/__tests__/jwt.test.ts
git commit -m "feat(api): add JWT, callerInfo, errorHandler plugins"
```

---

### Task 9: @vegabase/api — Argon2idHasher

**Files:**
- Create: `packages/api/src/password/password-hasher.ts`
- Create: `packages/api/src/password/argon2id-hasher.ts`
- Test: `packages/api/src/__tests__/argon2id-hasher.test.ts`

- [ ] **Step 1: Write the failing test**

`packages/api/src/__tests__/argon2id-hasher.test.ts`:
```typescript
import { describe, it, expect } from 'vitest';
import { Argon2idHasher } from '../password/argon2id-hasher';

describe('Argon2idHasher', () => {
  // Use low-cost params so tests run fast
  const hasher = new Argon2idHasher({ timeCost: 1, memoryCost: 8192, parallelism: 1 });

  it('hash_returnsHashedString', async () => {
    const hash = await hasher.hash('password123');
    expect(hash).toBeTruthy();
    expect(hash).not.toBe('password123');
  });

  it('hash_differentCallsReturnDifferentHashes', async () => {
    const hash1 = await hasher.hash('password123');
    const hash2 = await hasher.hash('password123');
    expect(hash1).not.toBe(hash2); // argon2 uses random salt
  });

  it('verify_correctPassword_returnsTrue', async () => {
    const hash = await hasher.hash('mySecret');
    const result = await hasher.verify('mySecret', hash);
    expect(result).toBe(true);
  });

  it('verify_wrongPassword_returnsFalse', async () => {
    const hash = await hasher.hash('mySecret');
    const result = await hasher.verify('wrongPassword', hash);
    expect(result).toBe(false);
  });
}, { timeout: 30_000 });
```

- [ ] **Step 2: Run test to verify it fails**

```bash
pnpm --filter @vegabase/api test
```

Expected: FAIL — `Cannot find module '../password/argon2id-hasher'`

- [ ] **Step 3: Implement**

`packages/api/src/password/password-hasher.ts`:
```typescript
export interface PasswordHasher {
  hash(password: string): Promise<string>;
  verify(password: string, hash: string): Promise<boolean>;
}
```

`packages/api/src/password/argon2id-hasher.ts`:
```typescript
import argon2 from 'argon2';
import type { PasswordHasher } from './password-hasher';

export interface Argon2Options {
  timeCost?: number;
  memoryCost?: number;
  parallelism?: number;
  hashLength?: number;
}

export class Argon2idHasher implements PasswordHasher {
  constructor(private readonly options: Argon2Options = {}) {}

  async hash(password: string): Promise<string> {
    return argon2.hash(password, {
      type: argon2.argon2id,
      timeCost: this.options.timeCost ?? 3,
      memoryCost: this.options.memoryCost ?? 65536,
      parallelism: this.options.parallelism ?? 4,
      hashLength: this.options.hashLength ?? 32,
    });
  }

  async verify(password: string, hash: string): Promise<boolean> {
    return argon2.verify(hash, password);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
pnpm --filter @vegabase/api test
```

Expected: PASS — 4 tests pass. Note: these tests are slow (~2-5s each) due to argon2 hashing.

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/password/ packages/api/src/__tests__/argon2id-hasher.test.ts
git commit -m "feat(api): add Argon2idHasher with configurable OWASP parameters"
```

---

### Task 10: @vegabase/api — createBaseController + buildParam

**Files:**
- Create: `packages/api/src/controllers/create-base-controller.ts`
- Create: `packages/api/src/index.ts`
- Test: `packages/api/src/__tests__/create-base-controller.test.ts`

- [ ] **Step 1: Write the failing test**

`packages/api/src/__tests__/create-base-controller.test.ts`:
```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest';
import Fastify from 'fastify';
import { z } from 'zod';
import { createBaseController } from '../controllers/create-base-controller';
import type { BaseService, BaseParamModel } from '@vegabase/service';
import type { BaseEntity } from '@vegabase/core';
import { ok, fail } from '@vegabase/core';

interface UserEntity extends BaseEntity {
  name: string;
}

interface UserParam extends BaseParamModel {
  name?: string;
}

const userSchema = z.object({
  id: z.string().optional(),
  name: z.string().optional(),
  updatedFields: z.array(z.string()).optional(),
  page: z.coerce.number().optional(),
  pageSize: z.coerce.number().optional(),
});

function makeService(): BaseService<UserEntity, UserParam> {
  return {
    getList: vi.fn(),
    add: vi.fn(),
    updateField: vi.fn(),
    delete: vi.fn(),
  } as unknown as BaseService<UserEntity, UserParam>;
}

function makeEntity(overrides: Partial<UserEntity> = {}): UserEntity {
  return { id: 'eid', name: 'Alice', isDeleted: false, logCreatedDate: new Date(), logCreatedBy: 'u', logUpdatedDate: null, logUpdatedBy: null, ...overrides };
}

async function buildApp(service: BaseService<UserEntity, UserParam>) {
  const app = Fastify();
  // Simulate caller middleware without JWT
  app.addHook('preHandler', async req => {
    (req as typeof req & { caller: { username: string; roles: string[] } }).caller = Object.freeze({
      username: 'testuser',
      roles: ['ADMIN'],
    });
  });
  app.decorateRequest('caller', null);
  await app.register(
    createBaseController({ service, prefix: '/users', schemas: { add: userSchema, update: userSchema } }),
  );
  await app.ready();
  return app;
}

describe('createBaseController', () => {
  let service: BaseService<UserEntity, UserParam>;
  let app: ReturnType<typeof Fastify>;

  beforeEach(async () => {
    service = makeService();
    app = await buildApp(service);
  });

  it('POST /add — success returns 201 with data', async () => {
    vi.mocked(service.add).mockResolvedValue(ok(makeEntity()));

    const res = await app.inject({ method: 'POST', url: '/users/add', payload: { name: 'Alice' } });

    expect(res.statusCode).toBe(201);
    const body = res.json<{ success: boolean; data: UserEntity }>();
    expect(body.success).toBe(true);
    expect(body.data.name).toBe('Alice');
  });

  it('POST /add — callerUsername comes from middleware not body', async () => {
    vi.mocked(service.add).mockResolvedValue(ok(makeEntity()));
    let capturedParam: UserParam | null = null;
    vi.mocked(service.add).mockImplementation(async p => { capturedParam = p; return ok(makeEntity()); });

    await app.inject({ method: 'POST', url: '/users/add', payload: { name: 'Alice', callerUsername: 'HACKED' } });

    expect(capturedParam!.callerUsername).toBe('testuser'); // from middleware, not body
  });

  it('POST /add — service error maps to correct HTTP status', async () => {
    vi.mocked(service.add).mockResolvedValue(fail([{ code: 'PERMISSION_DENIED', message: 'Access denied.' }]));

    const res = await app.inject({ method: 'POST', url: '/users/add', payload: {} });

    expect(res.statusCode).toBe(403);
    const body = res.json<{ success: boolean }>();
    expect(body.success).toBe(false);
  });

  it('GET /list — success returns items and total', async () => {
    vi.mocked(service.getList).mockResolvedValue(ok({ items: [makeEntity()], total: 1 }));

    const res = await app.inject({ method: 'GET', url: '/users/list' });

    expect(res.statusCode).toBe(200);
    const body = res.json<{ data: { items: UserEntity[]; total: number } }>();
    expect(body.data.total).toBe(1);
  });

  it('DELETE /delete — NOT_FOUND returns 404', async () => {
    vi.mocked(service.delete).mockResolvedValue(fail([{ code: 'NOT_FOUND', message: 'Not found.' }]));

    const res = await app.inject({ method: 'DELETE', url: '/users/delete?id=missing' });

    expect(res.statusCode).toBe(404);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

```bash
pnpm --filter @vegabase/api test
```

Expected: FAIL — `Cannot find module '../controllers/create-base-controller'`

- [ ] **Step 3: Implement createBaseController**

`packages/api/src/controllers/create-base-controller.ts`:
```typescript
import type { FastifyPluginAsync, FastifyRequest } from 'fastify';
import type { ZodSchema } from 'zod';
import { successResponse, failResponse, type BaseEntity } from '@vegabase/core';
import type { BaseService, BaseParamModel } from '@vegabase/service';
import type { CallerInfo } from '../plugins/caller-info';

const STATUS_MAP: Record<string, number> = {
  VALIDATION: 400,
  PERMISSION_DENIED: 403,
  NOT_FOUND: 404,
  DUPLICATE_KEY: 409,
  DB_TIMEOUT: 503,
  UNKNOWN: 500,
};

function errorsToStatus(errors: { code: string }[]): number {
  return STATUS_MAP[errors[0]?.code ?? 'UNKNOWN'] ?? 500;
}

function buildParam<TParam extends BaseParamModel>(
  req: FastifyRequest & { caller: CallerInfo },
  clientData: unknown,
  schema: ZodSchema<TParam>,
): TParam {
  const parsed = schema.parse(clientData);
  return {
    ...parsed,
    callerUsername: req.caller.username,
    callerRoles: req.caller.roles,
  };
}

export interface BaseControllerOptions<TModel extends BaseEntity, TParam extends BaseParamModel> {
  service: BaseService<TModel, TParam>;
  prefix: string;
  schemas: {
    list?: ZodSchema<TParam>;
    add: ZodSchema<TParam>;
    update: ZodSchema<TParam>;
    delete?: ZodSchema<TParam>;
  };
}

export function createBaseController<TModel extends BaseEntity, TParam extends BaseParamModel>(
  options: BaseControllerOptions<TModel, TParam>,
): FastifyPluginAsync {
  return async app => {
    const req = (r: FastifyRequest) => r as FastifyRequest & { caller: CallerInfo };

    app.get(`${options.prefix}/list`, async (r, reply) => {
      const traceId = crypto.randomUUID();
      const schema = options.schemas.list ?? options.schemas.add;
      const param = buildParam(req(r), r.query, schema);
      const result = await options.service.getList(param);
      if (!result.ok) return reply.status(errorsToStatus(result.errors)).send(failResponse(result.errors, traceId));
      return reply.send(successResponse(result.data, traceId));
    });

    app.post(`${options.prefix}/add`, async (r, reply) => {
      const traceId = crypto.randomUUID();
      const param = buildParam(req(r), r.body, options.schemas.add);
      const result = await options.service.add(param);
      if (!result.ok) return reply.status(errorsToStatus(result.errors)).send(failResponse(result.errors, traceId));
      return reply.status(201).send(successResponse(result.data, traceId));
    });

    app.put(`${options.prefix}/update-field`, async (r, reply) => {
      const traceId = crypto.randomUUID();
      const param = buildParam(req(r), r.body, options.schemas.update);
      const result = await options.service.updateField(param);
      if (!result.ok) return reply.status(errorsToStatus(result.errors)).send(failResponse(result.errors, traceId));
      return reply.send(successResponse(result.data, traceId));
    });

    app.delete(`${options.prefix}/delete`, async (r, reply) => {
      const traceId = crypto.randomUUID();
      const schema = options.schemas.delete ?? options.schemas.update;
      const param = buildParam(req(r), r.query, schema);
      const result = await options.service.delete(param);
      if (!result.ok) return reply.status(errorsToStatus(result.errors)).send(failResponse(result.errors, traceId));
      return reply.send(successResponse(result.data, traceId));
    });
  };
}
```

- [ ] **Step 4: Create API package index**

`packages/api/src/index.ts`:
```typescript
export { vegabaseJwtPlugin, type JwtConfig } from './plugins/jwt';
export { callerInfoPlugin, type CallerInfo } from './plugins/caller-info';
export { errorHandlerPlugin } from './plugins/error-handler';
export type { PasswordHasher } from './password/password-hasher';
export { Argon2idHasher, type Argon2Options } from './password/argon2id-hasher';
export { createBaseController, type BaseControllerOptions } from './controllers/create-base-controller';
```

- [ ] **Step 5: Run all tests across all packages**

```bash
cd e:/Work/vegabase-node
pnpm test
```

Expected: PASS — all tests across `@vegabase/core`, `@vegabase/service`, `@vegabase/api` pass.

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/controllers/ packages/api/src/index.ts packages/api/src/__tests__/create-base-controller.test.ts
git commit -m "feat(api): add createBaseController factory with buildParam security + HTTP status mapping"
```

---

## Self-Review

**Spec coverage:**

| Spec requirement | Covered by |
|---|---|
| `Result<T>` unified return | Task 2 — `result.ts` |
| `Errors` accumulator | Task 2 — `errors.ts` |
| `BaseEntity` interface | Task 2 — `base-entity.ts` |
| `ApiResponse<T>`, `PagedResult<T>` | Task 2 — `api-response.ts`, `paged-result.ts` |
| `BaseParamModel` + `hasField` | Task 3 |
| `DbResult<T>` discriminated union | Task 3 |
| `PrismaDelegate<T>` | Task 3 |
| `DbActionExecutor` with retry/timeout | Task 4 |
| `UnitOfWork` transaction support | Task 5 |
| `CacheStore<TKey, TModel>` with TTL | Task 6 |
| `PermissionCache` with TTL + `onInvalidate` | Task 6 |
| `BaseService<TModel, TParam>` 2 type params | Task 7 |
| `allowedUpdateFields` whitelist | Task 7 |
| Hook methods (`applyFilter`, `checkAddCondition`, etc.) | Task 7 |
| `buildNewEntity` abstract hook | Task 7 |
| `onChanged` pattern | Task 7 |
| JWT plugin + 401 on missing/expired token | Task 8 |
| `callerInfoPlugin` — frozen `req.caller` from JWT | Task 8 |
| `errorHandlerPlugin` — no stack trace, traceId | Task 8 |
| `Argon2idHasher` configurable options | Task 9 |
| `createBaseController` factory function | Task 10 |
| `buildParam` strips client caller fields | Task 10 — verified by test |
| HTTP status mapping (`PERMISSION_DENIED→403`, etc.) | Task 10 |
| All routes: GET /list, POST /add, PUT /update-field, DELETE /delete | Task 10 |

**Placeholder scan:** No TBDs. All steps have complete code.

**Type consistency:**
- `Errors` used in `base-service.ts` imported from `@vegabase/core` ✓
- `hasField` imported from `./models/base-param-model` in `base-service.ts` ✓
- `dbSuccess`/`dbFailure` factory names consistent across tasks 3–7 ✓
- `PrismaDelegate<T>` interface used consistently in executor and service ✓
- `CallerInfo` from `caller-info.ts` imported in `create-base-controller.ts` ✓
