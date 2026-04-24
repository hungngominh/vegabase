// VegaBase.Service/Infrastructure/DbActions/DbActionExecutor.cs
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using VegaBase.Core.Entities;

namespace VegaBase.Service.Infrastructure.DbActions;

public class DbActionExecutor : IDbActionExecutor
{
    private readonly DbContext _db;
    private readonly ILogger<DbActionExecutor> _logger;

    public DbActionExecutor(DbContext db, ILogger<DbActionExecutor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DbResult<TEntity?>> GetByIdAsync<TEntity>(Guid id, bool tracked = false, bool includeDeleted = false)
        where TEntity : BaseEntity
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var query = _db.Set<TEntity>().AsQueryable();
            if (!tracked) query = query.AsNoTracking();
            if (!includeDeleted) query = query.Where(e => !e.IsDeleted);
            var entity = await query.FirstOrDefaultAsync(e => e.Id == id);
            sw.Stop();
            _logger.LogDebug(
                "[DbAction] GetById {EntityType} {EntityId} {Status} in {DurationMs}ms",
                typeof(TEntity).Name, id, entity != null ? "found" : "not_found", sw.ElapsedMilliseconds);
            return DbResult<TEntity?>.Success(entity, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[DbAction] GetById {EntityType} {EntityId} FAILED in {DurationMs}ms",
                typeof(TEntity).Name, id, sw.ElapsedMilliseconds);
            return DbResult<TEntity?>.Failure(MapException(ex, "GetById"), sw.Elapsed);
        }
    }

    public async Task<DbResult<List<TEntity>>> QueryAsync<TEntity>(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        bool tracked = false)
        where TEntity : BaseEntity
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var query = _db.Set<TEntity>().AsQueryable();
            if (!tracked) query = query.AsNoTracking();
            query = queryBuilder(query);
            var results = await query.ToListAsync();
            sw.Stop();
            _logger.LogDebug(
                "[DbAction] Query {EntityType} returned {Count} rows in {DurationMs}ms",
                typeof(TEntity).Name, results.Count, sw.ElapsedMilliseconds);
            return DbResult<List<TEntity>>.Success(results, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[DbAction] Query {EntityType} FAILED in {DurationMs}ms",
                typeof(TEntity).Name, sw.ElapsedMilliseconds);
            return DbResult<List<TEntity>>.Failure(MapException(ex, "Query"), sw.Elapsed);
        }
    }

    public async Task<DbResult<TEntity>> AddAsync<TEntity>(TEntity entity, string createdBy)
        where TEntity : BaseEntity
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[DbAction] Add {EntityType} started by {User}", typeof(TEntity).Name, createdBy);
        try
        {
            entity.Log_CreatedBy = createdBy;
            entity.Log_CreatedDate = DateTimeOffset.UtcNow;
            _db.Set<TEntity>().Add(entity);
            await _db.SaveChangesAsync();
            sw.Stop();
            _logger.LogInformation(
                "[DbAction] Add {EntityType} {EntityId} OK in {DurationMs}ms",
                typeof(TEntity).Name, entity.Id, sw.ElapsedMilliseconds);
            return DbResult<TEntity>.Success(entity, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[DbAction] Add {EntityType} FAILED in {DurationMs}ms",
                typeof(TEntity).Name, sw.ElapsedMilliseconds);
            return DbResult<TEntity>.Failure(MapException(ex, "Add"), sw.Elapsed);
        }
    }

    public async Task<DbResult<List<TEntity>>> AddRangeAsync<TEntity>(List<TEntity> entities, string createdBy)
        where TEntity : BaseEntity
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[DbAction] AddRange {EntityType} x{Count} started by {User}",
            typeof(TEntity).Name, entities.Count, createdBy);
        try
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entity in entities)
            {
                entity.Log_CreatedBy = createdBy;
                entity.Log_CreatedDate = now;
            }
            _db.Set<TEntity>().AddRange(entities);
            await _db.SaveChangesAsync();
            sw.Stop();
            _logger.LogInformation(
                "[DbAction] AddRange {EntityType} x{Count} OK in {DurationMs}ms",
                typeof(TEntity).Name, entities.Count, sw.ElapsedMilliseconds);
            return DbResult<List<TEntity>>.Success(entities, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[DbAction] AddRange {EntityType} FAILED in {DurationMs}ms",
                typeof(TEntity).Name, sw.ElapsedMilliseconds);
            return DbResult<List<TEntity>>.Failure(MapException(ex, "AddRange"), sw.Elapsed);
        }
    }

    public async Task<DbResult<TEntity>> UpdateAsync<TEntity>(TEntity entity, string updatedBy)
        where TEntity : BaseEntity
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[DbAction] Update {EntityType} {EntityId} started by {User}",
            typeof(TEntity).Name, entity.Id, updatedBy);
        try
        {
            entity.Log_UpdatedBy = updatedBy;
            entity.Log_UpdatedDate = DateTimeOffset.UtcNow;
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            sw.Stop();
            _logger.LogInformation(
                "[DbAction] Update {EntityType} {EntityId} OK in {DurationMs}ms",
                typeof(TEntity).Name, entity.Id, sw.ElapsedMilliseconds);
            return DbResult<TEntity>.Success(entity, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[DbAction] Update {EntityType} {EntityId} FAILED in {DurationMs}ms",
                typeof(TEntity).Name, entity.Id, sw.ElapsedMilliseconds);
            return DbResult<TEntity>.Failure(MapException(ex, "Update"), sw.Elapsed);
        }
    }

    public async Task<DbResult<bool>> SoftDeleteAsync<TEntity>(TEntity entity, string deletedBy)
        where TEntity : BaseEntity
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[DbAction] SoftDelete {EntityType} {EntityId} started by {User}",
            typeof(TEntity).Name, entity.Id, deletedBy);
        try
        {
            entity.IsDeleted = true;
            entity.Log_UpdatedBy = deletedBy;
            entity.Log_UpdatedDate = DateTimeOffset.UtcNow;
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            sw.Stop();
            _logger.LogInformation(
                "[DbAction] SoftDelete {EntityType} {EntityId} OK in {DurationMs}ms",
                typeof(TEntity).Name, entity.Id, sw.ElapsedMilliseconds);
            return DbResult<bool>.Success(true, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[DbAction] SoftDelete {EntityType} {EntityId} FAILED in {DurationMs}ms",
                typeof(TEntity).Name, entity.Id, sw.ElapsedMilliseconds);
            return DbResult<bool>.Failure(MapException(ex, "SoftDelete"), sw.Elapsed);
        }
    }

    public async Task<DbResult<T>> ExecuteInTransactionAsync<T>(
        Func<IUnitOfWork, Task<T>> action, string operationName = "")
    {
        var sw = Stopwatch.StartNew();
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var uow = new UnitOfWork(_db, _logger);
            var result = await action(uow);
            await transaction.CommitAsync();
            sw.Stop();
            _logger.LogInformation(
                "[DbAction] Transaction {Operation} completed in {DurationMs}ms",
                operationName, sw.ElapsedMilliseconds);
            return DbResult<T>.Success(result, sw.Elapsed);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            sw.Stop();
            _logger.LogError(ex,
                "[DbAction] Transaction {Operation} FAILED in {DurationMs}ms",
                operationName, sw.ElapsedMilliseconds);
            return DbResult<T>.Failure(MapException(ex, "Transaction"), sw.Elapsed);
        }
    }

    internal static DbError MapException(Exception ex, string actionName)
    {
        var entityName = ex is DbUpdateException due && due.Entries.Count > 0
            ? due.Entries[0].Entity.GetType().Name
            : null;

        if (ex.InnerException is PostgresException pgEx)
        {
            var type = pgEx.SqlState switch
            {
                "23505" => DbErrorType.DuplicateKey,
                "23503" => DbErrorType.ForeignKeyViolation,
                "57014" => DbErrorType.Timeout,
                var s when s?.StartsWith("08") == true => DbErrorType.ConnectionError,
                _ => DbErrorType.Unknown
            };
            return new DbError
            {
                Type = type, Message = pgEx.MessageText,
                InnerException = ex, EntityName = entityName, ActionName = actionName
            };
        }

        if (ex.InnerException is { } inner &&
            inner.GetType().FullName?.EndsWith(".SqlException", StringComparison.Ordinal) == true)
        {
            var sqlEx = inner;
            var number = Convert.ToInt32(sqlEx.GetType().GetProperty("Number")?.GetValue(sqlEx) ?? 0);
            var type = number switch
            {
                2627 or 2601 => DbErrorType.DuplicateKey,
                547           => DbErrorType.ForeignKeyViolation,
                -2            => DbErrorType.Timeout,
                53            => DbErrorType.ConnectionError,
                _             => DbErrorType.Unknown
            };
            return new DbError
            {
                Type = type, Message = sqlEx.Message,
                InnerException = ex, EntityName = entityName, ActionName = actionName
            };
        }

        if (ex is DbUpdateConcurrencyException)
            return new DbError
            {
                Type = DbErrorType.ConcurrencyConflict, Message = ex.Message,
                InnerException = ex, EntityName = entityName, ActionName = actionName
            };

        return new DbError
        {
            Type = DbErrorType.Unknown, Message = ex.Message,
            InnerException = ex, EntityName = entityName, ActionName = actionName
        };
    }
}
