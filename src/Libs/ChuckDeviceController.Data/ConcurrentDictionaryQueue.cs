namespace ChuckDeviceController.Data
{
    using System.Collections.Concurrent;

    using Z.BulkOperations;

    public class ConcurrentDictionaryQueue<TEntity> : ConcurrentDictionary<BulkOperation<TEntity>, List<TEntity>>
        where TEntity : class
    {
        private const uint SemWaitTimeS = 3;

        //private readonly object _lock = new();
        private readonly SemaphoreSlim _sem = new(1, 1);

        public async Task<List<KeyValuePair<BulkOperation<TEntity>, List<TEntity>>>> TakeAllAsync()
        {
            await _sem.WaitAsync(TimeSpan.FromSeconds(SemWaitTimeS));
            var results = new List<KeyValuePair<BulkOperation<TEntity>, List<TEntity>>>(this);
            Clear();
            _sem.Release();
            return results;
        }
    }
}