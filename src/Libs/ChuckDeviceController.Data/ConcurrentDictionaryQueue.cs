namespace ChuckDeviceController.Data
{
    using System.Collections.Concurrent;

    public class ConcurrentDictionaryQueue<TKey, TEntity> : ConcurrentDictionary<TKey, TEntity>
        where TKey : notnull
        where TEntity : class
    {
        private readonly SemaphoreSlim _sem = new(1, 1);

        public async Task<List<KeyValuePair<TKey, TEntity>>> TakeAllAsync(CancellationToken stoppingToken = default)
        {
            await _sem.WaitAsync(stoppingToken);

            var results = new List<KeyValuePair<TKey, TEntity>>(this)
            {
                Capacity = int.MaxValue / 4,
            };
            Clear();

            _sem.Release();
            return results;
        }
    }
}