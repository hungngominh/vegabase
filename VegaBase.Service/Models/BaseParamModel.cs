namespace VegaBase.Service.Models;

/// <summary>
/// Base class for all service method parameter models.
/// CallerUsername and CallerRole are set by the controller from JWT claims.
/// </summary>
public class BaseParamModel
{
    public string CallerUsername { get; set; } = string.Empty;
    public string CallerRole { get; set; } = string.Empty;
    public List<Guid> CallerRoleIds { get; set; } = new();
    public Guid? Id { get; set; }
    public int Page     { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [System.Text.Json.Serialization.JsonIgnore]
    public int TotalCount { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public HashSet<string> UpdatedFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasField(string fieldName) =>
        UpdatedFields.Count == 0 || UpdatedFields.Contains(fieldName);
}
