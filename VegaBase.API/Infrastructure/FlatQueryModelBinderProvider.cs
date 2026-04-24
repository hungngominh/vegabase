using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VegaBase.Service.Models;

namespace VegaBase.API.Infrastructure;

/// <summary>
/// Model binder for types that extend <see cref="BaseParamModel"/> bound via <c>[FromQuery]</c>.
/// Reads every query-string key and maps it to a matching public property using reflection with
/// a per-type property cache. Falls back to the default binder for unknown keys.
/// </summary>
public sealed class FlatQueryModelBinder : IModelBinder
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propCache = new();

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelType = bindingContext.ModelType;
        var instance  = Activator.CreateInstance(modelType);
        if (instance is null) { bindingContext.Result = ModelBindingResult.Failed(); return Task.CompletedTask; }

        var props = _propCache.GetOrAdd(modelType, static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanWrite)
             .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase));

        foreach (var (key, prop) in props)
        {
            var value = bindingContext.HttpContext.Request.Query[key];
            if (value.Count == 0) continue;

            try
            {
                var targetType     = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var converter      = TypeDescriptor.GetConverter(targetType);

                if (converter.CanConvertFrom(typeof(string)))
                {
                    var converted = converter.ConvertFromInvariantString(value[0] ?? string.Empty);
                    prop.SetValue(instance, converted);
                }
            }
            catch
            {
                // Skip properties that fail type conversion — leave at default value.
            }
        }

        bindingContext.Result = ModelBindingResult.Success(instance);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Registers <see cref="FlatQueryModelBinder"/> for all <see cref="BaseParamModel"/> subtypes
/// bound via <c>[FromQuery]</c>. Add to <c>MvcOptions.ModelBinderProviders</c> at position 0.
/// </summary>
public sealed class FlatQueryModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.BindingInfo.BindingSource != BindingSource.Query) return null;
        if (!context.Metadata.ModelType.IsSubclassOf(typeof(BaseParamModel))) return null;

        return new FlatQueryModelBinder();
    }
}
