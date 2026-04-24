// VegaBase.Core/Entities/BaseEntity.cs
using System.ComponentModel.DataAnnotations;

namespace VegaBase.Core.Entities;

public abstract class BaseEntity
{
    /// <summary>UUIDv7 — time-ordered, globally unique. Generated on creation.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset Log_CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public string Log_CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? Log_UpdatedDate { get; set; }
    public string? Log_UpdatedBy { get; set; }

    /// <summary>
    /// Optimistic concurrency token. SQL Server maps to rowversion/timestamp automatically.
    /// PostgreSQL: configure HasRowVersion() or map to xmin in the consumer's DbContext.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
