using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using VegaBase.Service.Infrastructure.DbActions;

namespace VegaBase.Service.Infrastructure.Cache;

/// <summary>
/// Generic async in-memory cache base implementation using ConcurrentDictionary.
/// Features: async API (E12), per-key single-flight (E1), optional size cap (E7),
/// negative caching (E8), snapshot-safe bulk reload (E3), default-key filtering (E4).
/// </summary>
public class MemoryCacheStore<TKey, TCacheModel> : ICacheStore<TKey, TCacheModel>
    where TKey : notnull
{
    protected readonly ConcurrentDictionary<TKey, TCacheModel> _store = new();

    // Negative cache: tracks keys whose loader returned null to avoid repeated DB hits.
    private readonly ConcurrentDictionary<TKey, byte> _negativeKeys = new();

    // Single-flight: only one Task per key runs concurrently; subsequent callers await the same Task.
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TCacheModel?>>> _inflight = new();

    private volatile bool _allLoaded = false;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly int _maxSize;

    protected MemoryCacheStore(int maxSize = int.MaxValue) { _maxSize = maxSize; }

    protected MemoryCacheStore(IServiceScopeFactory scopeFactory, int maxSize = int.MaxValue)
    {
        _scopeFactory = scopeFactory;
        _maxSize      = maxSize;
    }

    /// <inheritdoc/>
    public bool IsWarmingEnabled => _scopeFactory != null;

    /// <inheritdoc/>
    public async Task<TCacheModel?> GetItemAsync(TKey key, Func<TKey, Task<TCacheModel?>> loaderAsync, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var hit)) return hit;
        if (_negativeKeys.ContainsKey(key)) return default;

        // Single-flight: GetOrAdd ensures only one Lazy<Task<>> is created per key.
        var lazy = _inflight.GetOrAdd(key, k => new Lazy<Task<TCacheModel?>>(() => loaderAsync(k)));
        try
        {
            var result = await lazy.Value.WaitAsync(ct);
            if (result is null)
            {
                _negativeKeys.TryAdd(key, 0);
                return default;
            }

            if (_store.Count < _maxSize)
                _store.TryAdd(key, result);

            return result;
        }
        finally
        {
            // Only remove if we still own this entry — prevents a cancelled caller from
            // evicting a Lazy that a concurrent caller is already awaiting.
            _inflight.TryRemove(new KeyValuePair<TKey, Lazy<Task<TCacheModel?>>>(key, lazy));
        }
    }

    /// <inheritdoc/>
    public async Task<List<TCacheModel>> GetAllAsync(Func<Task<List<TCacheModel>>> loaderAsync, CancellationToken ct = default)
    {
        if (_allLoaded) return _store.Values.ToList();

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_allLoaded) return _store.Values.ToList();

            // Run loader BEFORE clearing so a loader failure keeps the previous snapshot (E3).
            var fresh = await loaderAsync();

            _store.Clear();
            _negativeKeys.Clear();
            foreach (var item in fresh)
            {
                var key = ExtractKey(item);
                if (key is null) continue;
                if (EqualityComparer<TKey>.Default.Equals(key, default!)) continue;
                if (_store.Count < _maxSize)
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

    /// <summary>Remove a single entry. Does NOT force a full reload on the next <see cref="GetAllAsync"/>.</summary>
    public void Invalidate(TKey key)
    {
        _store.TryRemove(key, out _);
        _negativeKeys.TryRemove(key, out _);
    }

    public void InvalidateAll()
    {
        _store.Clear();
        _negativeKeys.Clear();
        _allLoaded = false;
    }

    protected virtual TKey? ExtractKey(TCacheModel item) => default;

    protected virtual Task<List<TCacheModel>> LoadAllAsync(IDbActionExecutor executor) => Task.FromResult(new List<TCacheModel>());

    /// <inheritdoc/>
    public async Task WarmAsync()
    {
        if (_scopeFactory == null) return;
        await GetAllAsync(async () =>
        {
            using var scope    = _scopeFactory.CreateScope();
            var executor       = scope.ServiceProvider.GetRequiredService<IDbActionExecutor>();
            return await LoadAllAsync(executor);
        });
    }
}
