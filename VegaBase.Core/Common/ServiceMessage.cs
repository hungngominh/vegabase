// VegaBase.Core/Common/ServiceMessage.cs
namespace VegaBase.Core.Common;

public class ServiceMessage
{
    public string Value { get; set; } = string.Empty;
    public bool HasError => !string.IsNullOrEmpty(Value);

    public static ServiceMessage operator +(ServiceMessage msg, string error)
    {
        if (!string.IsNullOrEmpty(error) && string.IsNullOrEmpty(msg.Value))
            return new ServiceMessage { Value = error };
        return msg;
    }
}
