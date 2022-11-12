namespace ChuckDeviceController.Extensions.Http.Caching
{
    public interface IMemoryCacheHostedService
    {
        TEntity? Get<TKey, TEntity>(TKey key);

        void Set<TKey, TEntity>(TKey key, TEntity obj, TimeSpan? expiry = null);

        void Unset<TKey, TEntity>(TKey key);

        bool IsSet<TKey, TEntity>(TKey key);
    }
}