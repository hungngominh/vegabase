// VegaBase.Service/Infrastructure/DbActions/UnitOfWork.cs
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VegaBase.Core.Entities;

namespace VegaBase.Service.Infrastructure.DbActions;

internal class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _db;
    private readonly ILogger _logger;
    private readonly string _traceId;

    public UnitOfWork(DbContext db, ILogger logger, string traceId = "")
    {
        _db = db;
        _logger = logger;
        _traceId = traceId;
    }

    public void Add<TEntity>(TEntity entity, string createdBy) where TEntity : BaseEntity
    {
        entity.Log_CreatedBy = createdBy;
        entity.Log_CreatedDate = DateTimeOffset.UtcNow;
        _db.Set<TEntity>().Add(entity);
    }

    public void AddRange<TEntity>(List<TEntity> entities, string createdBy) where TEntity : BaseEntity
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entity in entities)
        {
            entity.Log_CreatedBy = createdBy;
            entity.Log_CreatedDate = now;
        }
        _db.Set<TEntity>().AddRange(entities);
    }

    public void Update<TEntity>(TEntity entity, string updatedBy) where TEntity : BaseEntity
    {
        entity.Log_UpdatedBy = updatedBy;
        entity.Log_UpdatedDate = DateTimeOffset.UtcNow;
        _db.Entry(entity).State = EntityState.Modified;
    }

    public void SoftDelete<TEntity>(TEntity entity, string deletedBy) where TEntity : BaseEntity
    {
        entity.IsDeleted = true;
        entity.Log_UpdatedBy = deletedBy;
        entity.Log_UpdatedDate = DateTimeOffset.UtcNow;
        _db.Entry(entity).State = EntityState.Modified;
    }

    public async Task<DbResult<int>> SaveAsync(string operationName = "", CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var count = await _db.SaveChangesAsync(ct);
            sw.Stop();
            _logger.LogInformation(
                "[DbAction] UnitOfWork.Save {Operation} saved {Count} changes in {DurationMs}ms TraceId={TraceId}",
                operationName, count, sw.ElapsedMilliseconds, _traceId);
            return DbResult<int>.Success(count, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[DbAction] UnitOfWork.Save {Operation} FAILED in {DurationMs}ms TraceId={TraceId}",
                operationName, sw.ElapsedMilliseconds, _traceId);
            return DbResult<int>.Failure(DbActionExecutor.MapException(ex, operationName), sw.Elapsed);
        }
    }
}
