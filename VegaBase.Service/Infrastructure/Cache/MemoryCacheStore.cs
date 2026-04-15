using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using VegaBase.Service.Infrastructure.DbActions;

namespace VegaBase.Service.Infrastructure.Cache;

/// <summary>
/// Generic in-memory cache base implementation using ConcurrentDictionary.
/// Self-warming (opt-in): subclass receives IServiceScopeFactory, overrides LoadAll(),
/// and calls Warm() once at startup.
/// </summary>
public class MemoryCacheStore<TKey, TCacheModel> : ICacheStore<TKey, TCacheModel>
    where TKey : notnull
{
    protected readonly ConcurrentDictionary<TKey, TCacheModel> _store = new();
    private volatile bool _allLoaded = false;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly IServiceScopeFactory? _scopeFactory;

    protected MemoryCacheStore() { }

    protected MemoryCacheStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public TCacheModel? GetItem(TKey key, Func<TKey, TCacheModel?> loader)
    {
        if (_store.TryGetValue(key, out var hit)) return hit;
        var loaded = loader(key);
        if (loaded != null) _store[key] = loaded;
        return loaded;
    }

    public List<TCacheModel> GetAll(Func<List<TCacheModel>> loader)
    {
        if (_allLoaded) return _store.Values.ToList();

        _loadLock.Wait();
        try
        {
            if (_allLoaded) return _store.Values.ToList();

            _store.Clear();
            foreach (var item in loader())
                if (ExtractKey(item) is { } key)
                    _store[key] = item;

            _allLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }

        return _store.Values.ToList();
    }

    public void Invalidate(TKey key)
    {
        _store.TryRemove(key, out _);
        _allLoaded = false;
    }

    public void InvalidateAll()
    {
        _store.Clear();
        _allLoaded = false;
    }

    protected virtual TKey? ExtractKey(TCacheModel item) => default;

    protected virtual List<TCacheModel> LoadAll(IDbActionExecutor executor) => [];

    public void Warm()
    {
        if (_scopeFactory == null) return;
        GetAll(() =>
        {
            using var scope = _scopeFactory.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IDbActionExecutor>();
            return LoadAll(executor);
        });
    }
}
