namespace VegaBase.Service.Models;

/// <summary>
/// Base class for all service method parameter models.
/// CallerUsername and CallerRole are set by the controller from JWT claims.
/// </summary>
public class BaseParamModel
{
    public const int MaxPageSize = 1000;

    private int _page = 1;
    private int _pageSize = 20;

    public string CallerUsername { get; set; } = string.Empty;
    public string CallerRole { get; set; } = string.Empty;
    public List<Guid> CallerRoleIds { get; set; } = new();
    public Guid? Id { get; set; }

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public int TotalCount { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public HashSet<string> UpdatedFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true when <paramref name="fieldName"/> is present in <see cref="UpdatedFields"/>.
    /// v2 semantic: empty UpdatedFields means NO fields are updated (was "all fields" in v1).
    /// </summary>
    public bool HasField(string fieldName) => UpdatedFields.Contains(fieldName);
}
