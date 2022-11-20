namespace ChuckDeviceController.Data
{
    using System.Collections.Concurrent;

    //public class ConcurrentDictionaryQueue<TKey, TEntity> : ConcurrentDictionary<TKey, ConcurrentBag<TEntity>>
    //    where TKey : notnull
    //    where TEntity : class
    //{
    //    private const int MaxCapacity = 1024 * 1024;

    //    private readonly SemaphoreSlim _sem = new(1, 1);

    //    public async Task<List<KeyValuePair<TKey, ConcurrentBag<TEntity>>>> TakeAllAsync(CancellationToken stoppingToken = default)
    //    {
    //        await _sem.WaitAsync(stoppingToken);

    //        var results = new List<KeyValuePair<TKey, ConcurrentBag<TEntity>>>(this)
    //        {
    //            Capacity = MaxCapacity,
    //        };
    //        Clear();

    //        _sem.Release();
    //        return results;
    //    }
    //}

    public static class ConcurrentDictionaryExtensions
    {
        private static readonly SemaphoreSlim _sem = new(1, 1);

        public static async Task<SortedDictionary<SqlQueryType, ConcurrentBag<TEntity>>> TakeAllAsync<TEntity>(
            this ConcurrentDictionary<SqlQueryType, ConcurrentBag<TEntity>> dict,
            CancellationToken stoppingToken = default)
        {
            await _sem.WaitAsync(stoppingToken);

            var cloned = dict.ToDictionary(x => x.Key, y => y.Value);
            dict.Clear();

            _sem.Release();
            var sorted = new SortedDictionary<SqlQueryType, ConcurrentBag<TEntity>>(cloned, new SqlQueryTypeComparer());
            return sorted;
        }
    }

    public class SqlQueryTypeComparer : IComparer<SqlQueryType>
    {
        public int Compare(SqlQueryType x, SqlQueryType y) => ((int)x).CompareTo((int)y);
    }
}