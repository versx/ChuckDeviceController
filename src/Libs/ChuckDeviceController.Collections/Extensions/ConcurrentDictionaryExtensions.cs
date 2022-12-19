namespace ChuckDeviceController.Collections.Extensions;

using System.Collections.Concurrent;

public static class ConcurrentDictionaryExtensions
{
    private static readonly SemaphoreSlim _sem = new(1, 1);
    private static readonly object _lock = new();

    public static int GetCount<TKey, TEntity>(this ConcurrentDictionary<TKey, ConcurrentBag<TEntity>> dict)
    {
        lock (_lock)
        {
            var count = dict.Values.Sum(x => x.Count);
            return count;
        }
    }

    public static SortedDictionary<TKey, ConcurrentBag<TEntity>> TakeAll<TKey, TEntity>(
        this ConcurrentDictionary<TKey, ConcurrentBag<TEntity>> dict,
        IComparer<TKey> comparer)
        where TKey : notnull
    {
        lock (_lock)
        {
            var sorted = dict
                .ToSorted(comparer);
            dict.Clear();

            return sorted;
        }
    }

    public static async Task<SortedDictionary<TKey, ConcurrentBag<TEntity>>> TakeAllAsync<TKey, TEntity>(
        this ConcurrentDictionary<TKey, ConcurrentBag<TEntity>> dict,
        IComparer<TKey> comparer,
        CancellationToken stoppingToken = default)
        where TKey : notnull
    {
        await _sem.WaitAsync(stoppingToken);

        var sorted = dict
            .ToSorted(comparer);
        dict.Clear();

        _sem.Release();
        return sorted;
    }

    public static SortedDictionary<TKey, ConcurrentBag<TEntity>> Take<TKey, TEntity>(
        this ConcurrentDictionary<TKey, ConcurrentBag<TEntity>> dict,
        IComparer<TKey> comparer,
        int batchSize = 1000)
        where TKey : notnull
    {
        lock (_lock)
        {
            var dictList = new List<KeyValuePair<TKey, TEntity>>(dict
                .SelectMany(x => x.Value.Select(y => KeyValuePair.Create(x.Key, y)))
                .ToList()
            );

            var cloned = new SafeCollection<KeyValuePair<TKey, TEntity>>(dictList);
            var items = cloned.Take(batchSize);
            var sorted = items
                .GroupBy(g => g.Key, g => g.Value)
                .ToDictionary(x => x.Key, y => new ConcurrentBag<TEntity>(y.ToList()))
                .ToSorted(comparer);

            dict.Remove(items.Select(x => x.Value));

            return sorted;
        }
    }

    public static async Task<SortedDictionary<TKey, ConcurrentBag<TEntity>>> TakeAsync<TKey, TEntity>(
        this ConcurrentDictionary<TKey, ConcurrentBag<TEntity>> dict,
        IComparer<TKey> comparer,
        int batchSize = 1000,
        CancellationToken stoppingToken = default)
        where TKey : notnull
    {
        await _sem.WaitAsync(stoppingToken);

        var dictList = new List<KeyValuePair<TKey, TEntity>>(dict
            .SelectMany(x => x.Value.Select(y => KeyValuePair.Create(x.Key, y)))
            .ToList()
        );

        var cloned = new SafeCollection<KeyValuePair<TKey, TEntity>>(dictList);
        var items = cloned.Take(batchSize);
        var sorted = items
            .GroupBy(g => g.Key, g => g.Value)
            .ToDictionary(x => x.Key, y => new ConcurrentBag<TEntity>(y.ToList()))
            .ToSorted(comparer);

        dict.Remove(items.Select(x => x.Value));

        _sem.Release();
        return sorted;
    }

    public static bool Remove<TKey, TEntity>(
        this ConcurrentDictionary<TKey, ConcurrentBag<TEntity>> dict,
        IEnumerable<TEntity> items)
        where TKey : notnull
    {
        try
        {
            var list = new List<KeyValuePair<TKey, ConcurrentBag<TEntity>>>(dict.ToList());
            foreach (var (sqlQueryType, entities) in list)
            {
                if (!dict.ContainsKey(sqlQueryType))
                    continue;

                dict[sqlQueryType] = new(dict[sqlQueryType].Except(items));
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}