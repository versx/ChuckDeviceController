namespace ChuckDeviceController.Data
{
    using System.Collections.Concurrent;
    using System.Collections.Immutable;

    using ChuckDeviceController.Data.Entities;

    public class ConcurrentDictionaryQueue<TKey, TEntity> : ConcurrentDictionary<TKey, ConcurrentBag<TEntity>>
        where TKey : notnull
        where TEntity : class
    {
        private const int MaxCapacity = 1024 * 1024;

        private readonly SemaphoreSlim _sem = new(1, 1);

        public async Task<List<KeyValuePair<TKey, ConcurrentBag<TEntity>>>> TakeAllAsync(CancellationToken stoppingToken = default)
        {
            await _sem.WaitAsync(stoppingToken);

            var results = new List<KeyValuePair<TKey, ConcurrentBag<TEntity>>>(this)
            {
                Capacity = MaxCapacity,
            };
            Clear();

            _sem.Release();
            return results;
        }
    }

    public static class ConcurrentDictionaryExtensions
    {
        private static readonly SemaphoreSlim _sem = new(1, 1);
        private static object _lock = new();

        public static ConcurrentDictionary<TKey, TValue> TakeAll<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dict,
            CancellationToken stoppingToken = default)
            where TKey : notnull
            //where TValue : class
        {
            _sem.Wait(stoppingToken);

            //lock (_lock)
            //{
                var clone = new ConcurrentDictionary<TKey, TValue>(dict);
                dict.Clear();

                _sem.Release();
                return clone;
            //}
        }

        //public static async Task<ConcurrentDictionary<TKey, TValue>> TakeAllAsync<TKey, TValue>(
        public static async Task<ImmutableSortedDictionary<SqlQueryType, ConcurrentBag<BaseEntity>>> TakeAllAsync(
            this ConcurrentDictionary<SqlQueryType, ConcurrentBag<BaseEntity>> dict,
            CancellationToken stoppingToken = default)
        {
            await _sem.WaitAsync(stoppingToken);

            var sortedClone = new ConcurrentDictionary<SqlQueryType, ConcurrentBag<BaseEntity>>(dict)
                .ToImmutableSortedDictionary(new SqlQueryTypeComparerer());
            dict.Clear();

            _sem.Release();
            return sortedClone;
        }
    }

    public class SqlQueryTypeComparerer : IComparer<SqlQueryType>
    {
        public int Compare(SqlQueryType x, SqlQueryType y)
        {
            return ((int)x).CompareTo((int)y);
        }
    }
}