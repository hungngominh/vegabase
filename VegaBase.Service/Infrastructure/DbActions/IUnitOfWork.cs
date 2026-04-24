// VegaBase.Service/Infrastructure/DbActions/IUnitOfWork.cs
using VegaBase.Core.Entities;

namespace VegaBase.Service.Infrastructure.DbActions;

public interface IUnitOfWork
{
    void Add<TEntity>(TEntity entity, string createdBy) where TEntity : BaseEntity;
    void AddRange<TEntity>(List<TEntity> entities, string createdBy) where TEntity : BaseEntity;
    void Update<TEntity>(TEntity entity, string updatedBy) where TEntity : BaseEntity;
    void SoftDelete<TEntity>(TEntity entity, string deletedBy) where TEntity : BaseEntity;
    Task<DbResult<int>> SaveAsync(string operationName = "", CancellationToken ct = default);
}
