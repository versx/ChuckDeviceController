namespace ChuckDeviceController.Caching.Memory;

public interface IMemoryCacheService
{
    TEntity? Get<TKey, TEntity>(TKey key)
        where TKey : notnull;

    void Set<TKey, TEntity>(TKey key, TEntity obj, TimeSpan? expiry = null)
        where TKey : notnull
        where TEntity : class;

    void Unset<TKey, TEntity>(TKey key)
        where TKey : notnull;

    bool IsSet<TKey, TEntity>(TKey key)
        where TKey : notnull;

    bool Clear<TEntity>();
}