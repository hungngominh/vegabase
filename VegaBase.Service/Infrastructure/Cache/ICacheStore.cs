namespace VegaBase.Service.Infrastructure.Cache;

/// <summary>
/// Generic in-memory key-value cache.
/// Only for lookup/master data that is small and rarely changes.
/// </summary>
public interface ICacheStore<TKey, TCacheModel> where TKey : notnull
{
    TCacheModel? GetItem(TKey key, Func<TKey, TCacheModel?> loader);
    List<TCacheModel> GetAll(Func<List<TCacheModel>> loader);
    void Invalidate(TKey key);
    void InvalidateAll();
    void Warm();
}
