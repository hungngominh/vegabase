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

    public static ApiResponse<T> Ok(T single) => new()
    {
        Success    = true,
        Data       = [single],
        Total      = 1,
        Page       = 1,
        PageSize   = 1,
        TotalPages = 1,
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message,
        Data    = [],
    };
}
