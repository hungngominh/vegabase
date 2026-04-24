// VegaBase.Service/Infrastructure/DbActions/IDbActionExecutor.cs
using VegaBase.Core.Entities;

namespace VegaBase.Service.Infrastructure.DbActions;

public interface IDbActionExecutor
{
    Task<DbResult<TEntity?>> GetByIdAsync<TEntity>(Guid id, bool tracked = false, bool includeDeleted = false)
        where TEntity : BaseEntity;

    Task<DbResult<List<TEntity>>> QueryAsync<TEntity>(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        bool tracked = false)
        where TEntity : BaseEntity;

    Task<DbResult<TEntity>> AddAsync<TEntity>(TEntity entity, string createdBy)
        where TEntity : BaseEntity;

    Task<DbResult<List<TEntity>>> AddRangeAsync<TEntity>(List<TEntity> entities, string createdBy)
        where TEntity : BaseEntity;

    Task<DbResult<TEntity>> UpdateAsync<TEntity>(TEntity entity, string updatedBy)
        where TEntity : BaseEntity;

    Task<DbResult<bool>> SoftDeleteAsync<TEntity>(TEntity entity, string deletedBy)
        where TEntity : BaseEntity;

    Task<DbResult<T>> ExecuteInTransactionAsync<T>(
        Func<IUnitOfWork, Task<T>> action,
        string operationName = "");
}
