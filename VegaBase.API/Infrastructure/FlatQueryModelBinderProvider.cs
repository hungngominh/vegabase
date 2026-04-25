using System.Collections;
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
            var values = bindingContext.HttpContext.Request.Query[key];
            if (values.Count == 0) continue;

            try
            {
                var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                // Multi-value path: List<T> / IEnumerable<T> / T[] / HashSet<T>
                var enumerableInterface = prop.PropertyType.GetInterfaces()
                    .Concat(new[] { prop.PropertyType })
                    .FirstOrDefault(i => i.IsGenericType
                                         && (i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                                             || i.GetGenericTypeDefinition() == typeof(ICollection<>)
                                             || i.GetGenericTypeDefinition() == typeof(IList<>)));
                if (prop.PropertyType != typeof(string) && enumerableInterface != null)
                {
                    var elementType = enumerableInterface.GetGenericArguments()[0];
                    var elementConverter = TypeDescriptor.GetConverter(elementType);
                    if (elementConverter.CanConvertFrom(typeof(string)))
                    {
                        var listType = typeof(List<>).MakeGenericType(elementType);
                        var list = (IList)Activator.CreateInstance(listType)!;
                        foreach (var v in values)
                        {
                            if (v is null) continue;
                            var item = elementConverter.ConvertFromInvariantString(v);
                            list.Add(item);
                        }
                        if (prop.PropertyType.IsArray)
                        {
                            var arr = Array.CreateInstance(elementType, list.Count);
                            list.CopyTo(arr, 0);
                            prop.SetValue(instance, arr);
                        }
                        else if (prop.PropertyType.IsAssignableFrom(listType))
                        {
                            prop.SetValue(instance, list);
                        }
                        else
                        {
                            // Try ctor that takes IEnumerable<elementType> (e.g. HashSet<T>)
                            var ctor = prop.PropertyType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
                            if (ctor != null) prop.SetValue(instance, ctor.Invoke(new object[] { list }));
                        }
                        continue;
                    }
                }

                // Single-value path
                var converter = TypeDescriptor.GetConverter(underlying);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    var converted = converter.ConvertFromInvariantString(values[0] ?? string.Empty);
                    prop.SetValue(instance, converted);
                }
            }
            catch (Exception ex)
            {
                // Fix #11: log instead of swallowing silently. ILogger from DI is not directly available
                // in IModelBinder — write to the binding context's ModelState as a debug aid.
                bindingContext.ModelState.TryAddModelError(key, $"Conversion failed: {ex.GetType().Name}");
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
