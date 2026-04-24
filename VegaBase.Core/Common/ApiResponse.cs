// VegaBase.Core/Common/ApiResponse.cs
namespace VegaBase.Core.Common;

public class ApiResponse<T>
{
    public bool   Success    { get; set; }
    public string Message    { get; set; } = string.Empty;
    public T[]    Data       { get; set; } = [];
    public int    Total      { get; set; }
    public int    Page       { get; set; }
    public int    PageSize   { get; set; }
    public int    TotalPages { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }

    public static ApiResponse<T> Ok(
        List<T>? data       = null,
        int      total      = 0,
        int      page       = 1,
        int      pageSize   = 20,
        int      totalPages = 0) => new()
    {
        Success    = true,
        Data       = data?.ToArray() ?? [],
        Total      = total,
        Page       = page,
        PageSize   = pageSize,
        TotalPages = totalPages,
    };

    public static ApiResponse<T> Ok(T single)
    {
        if (single is null)
            return new ApiResponse<T>
            {
                Success    = true,
                Data       = [],
                Total      = 0,
                Page       = 1,
                PageSize   = 1,
                TotalPages = 0,
            };

        return new ApiResponse<T>
        {
            Success    = true,
            Data       = [single],
            Total      = 1,
            Page       = 1,
            PageSize   = 1,
            TotalPages = 1,
        };
    }

    public static ApiResponse<T> Fail(string message, string? traceId = null) => new()
    {
        Success = false,
        Message = message,
        Data    = [],
        TraceId = traceId,
    };
}
