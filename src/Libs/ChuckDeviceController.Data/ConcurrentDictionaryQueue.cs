namespace ChuckDeviceController.Data
{
    using System.Collections.Concurrent;

    using ChuckDeviceController.Collections;

    public static class ConcurrentDictionaryExtensions
    {
        private static readonly SemaphoreSlim _sem = new(1, 1);
        private static readonly object _lock = new();

        public static SortedDictionary<SqlQueryType, ConcurrentBag<TEntity>> TakeAll<TEntity>(
            this ConcurrentDictionary<SqlQueryType, ConcurrentBag<TEntity>> dict)
        {
            lock (_lock)
            {
                var sorted = dict
                    .ToDictionary(x => x.Key, y => y.Value)
                    .ToSorted();
                dict.Clear();

                return sorted;
            }
        }

        public static async Task<SortedDictionary<SqlQueryType, ConcurrentBag<TEntity>>> TakeAllAsync<TEntity>(
            this ConcurrentDictionary<SqlQueryType, ConcurrentBag<TEntity>> dict,
            CancellationToken stoppingToken = default)
        {
            await _sem.WaitAsync(stoppingToken);

            var sorted = dict
                .ToDictionary(x => x.Key, y => y.Value)
                .ToSorted();
            dict.Clear();

            _sem.Release();
            return sorted;
        }

        public static async Task<SortedDictionary<SqlQueryType, ConcurrentBag<TEntity>>> TakeAsync<TEntity>(
            this ConcurrentDictionary<SqlQueryType, ConcurrentBag<TEntity>> dict,
            int batchSize = 1000,
            CancellationToken stoppingToken = default)
        {
            await _sem.WaitAsync(stoppingToken);

            var dictList = new List<KeyValuePair<SqlQueryType, TEntity>>(dict
                .SelectMany(x => x.Value.Select(y => KeyValuePair.Create(x.Key, y)))
                .ToList()
            );

            var cloned = new SafeCollection<KeyValuePair<SqlQueryType, TEntity>>(dictList);
            var items = cloned.Take(batchSize);
            var sorted = items
                .GroupBy(g => g.Key, g => g.Value)
                .ToDictionary(x => x.Key, y => new ConcurrentBag<TEntity>(y.ToList()))
                .ToSorted();

            dict.Remove(items.Select(x => x.Value));

            _sem.Release();
            return sorted;
        }

        public static bool Remove<TEntity>(
            this ConcurrentDictionary<SqlQueryType, ConcurrentBag<TEntity>> dict,
            IEnumerable<TEntity> items)
        {
            try
            {
                var list = new List<KeyValuePair<SqlQueryType, ConcurrentBag<TEntity>>>(dict.ToList());
                foreach (var (sqlQueryType, entities) in list)
                {
                    if (!dict.ContainsKey(sqlQueryType))
                        continue;

                    dict[sqlQueryType] = new(dict[sqlQueryType].Except(items));
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public static class DictionaryExtensions
    {
        public static SortedDictionary<SqlQueryType, TValue> ToSorted<TValue>(this IDictionary<SqlQueryType, TValue> dict)
        {
            var sorted = new SortedDictionary<SqlQueryType, TValue>(dict, new SqlQueryTypeComparer());
            return sorted;
        }
    }

    public class SqlQueryTypeComparer : IComparer<SqlQueryType>
    {
        public int Compare(SqlQueryType x, SqlQueryType y) => ((int)x).CompareTo((int)y);
    }
}