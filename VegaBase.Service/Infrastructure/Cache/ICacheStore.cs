namespace VegaBase.Service.Infrastructure.Cache;

/// <summary>
/// Generic async in-memory key-value cache.
/// Only for lookup/master data that is small and rarely changes.
/// </summary>
public interface ICacheStore<TKey, TCacheModel> where TKey : notnull
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or invokes <paramref name="loaderAsync"/> exactly once
    /// per key under concurrent load (single-flight). Returns <c>null</c> when the loader returns null and
    /// negative caching is not enabled, or the previously negative-cached sentinel.
    /// </summary>
    Task<TCacheModel?> GetItemAsync(TKey key, Func<TKey, Task<TCacheModel?>> loaderAsync, CancellationToken ct = default);

    /// <summary>Returns all items, loading via <paramref name="loaderAsync"/> once on first call.</summary>
    Task<List<TCacheModel>> GetAllAsync(Func<Task<List<TCacheModel>>> loaderAsync, CancellationToken ct = default);

    void Invalidate(TKey key);
    void InvalidateAll();

    /// <summary>
    /// Pre-warm the cache at startup. No-op when no scope factory was supplied.
    /// See <see cref="IsWarmingEnabled"/>.
    /// </summary>
    Task WarmAsync();

    /// <summary>True if a scope factory was supplied and <see cref="WarmAsync"/> will actually load data.</summary>
    bool IsWarmingEnabled { get; }
}
