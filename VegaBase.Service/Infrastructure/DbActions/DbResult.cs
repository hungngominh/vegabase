// VegaBase.Service/Infrastructure/DbActions/DbResult.cs
namespace VegaBase.Service.Infrastructure.DbActions;

public class DbResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public DbError? Error { get; private init; }
    public TimeSpan Duration { get; private init; }

    public static DbResult<T> Success(T data, TimeSpan duration)
        => new() { IsSuccess = true, Data = data, Duration = duration };

    public static DbResult<T> Failure(DbError error, TimeSpan duration)
        => new() { IsSuccess = false, Error = error, Duration = duration };
}
