// VegaBase.Service/Infrastructure/DbActions/DbError.cs
namespace VegaBase.Service.Infrastructure.DbActions;

public class DbError
{
    public DbErrorType Type { get; init; }
    public string Message { get; init; } = "";
    public Exception? InnerException { get; init; }
    public string? EntityName { get; init; }
    public string? ActionName { get; init; }
}

public enum DbErrorType
{
    DuplicateKey,
    ForeignKeyViolation,
    ConcurrencyConflict,
    Timeout,
    ConnectionError,
    Unknown
}
