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

    /// <summary>
    /// True if a scope factory was supplied and <see cref="Warm"/> will actually load data.
    /// Consumers can assert this at startup to catch accidental use of the parameterless ctor.
    /// </summary>
    public bool IsWarmingEnabled => _scopeFactory != null;

    public TCacheModel? GetItem(TKey key, Func<TKey, TCacheModel?> loader)
    {
        if (_store.TryGetValue(key, out var hit)) return hit;
        var loaded = loader(key);
        if (loaded != null) _store.TryAdd(key, loaded);
        return loaded;
    }

    public List<TCacheModel> GetAll(Func<List<TCacheModel>> loader)
    {
        if (_allLoaded) return _store.Values.ToList();

        _loadLock.Wait();
        try
        {
            if (_allLoaded) return _store.Values.ToList();

            // Run the loader BEFORE clearing the store so a loader failure keeps
            // the previous snapshot intact (no window of empty cache during an outage).
            var fresh = loader();

            _store.Clear();
            foreach (var item in fresh)
            {
                var key = ExtractKey(item);
                if (key is null) continue;
                if (EqualityComparer<TKey>.Default.Equals(key, default!)) continue;
                _store[key] = item;
            }

            _allLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }

        return _store.Values.ToList();
    }

    /// <summary>Remove a single entry. Does NOT force a full reload on the next <see cref="GetAll"/>.</summary>
    public void Invalidate(TKey key)
    {
        _store.TryRemove(key, out _);
    }

    public void InvalidateAll()
    {
        _store.Clear();
        _allLoaded = false;
    }

    protected virtual TKey? ExtractKey(TCacheModel item) => default;

    protected virtual List<TCacheModel> LoadAll(IDbActionExecutor executor) => [];

    /// <summary>
    /// Warm the cache at startup. No-op when the parameterless constructor was used
    /// (see <see cref="IsWarmingEnabled"/>).
    /// </summary>
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
