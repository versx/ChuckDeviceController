namespace ChuckDeviceController.Collections.Queues;

using System.Collections.Concurrent;

public class AsyncQueue<T> : IAsyncQueue<T>
{
    private readonly SemaphoreSlim _sem = new(1, 1);
    //private readonly BlockingCollection<T> _queue;
    private readonly SafeCollection<T> _queue;

    public uint Count => Convert.ToUInt32(_queue?.Count ?? 0);

    public AsyncQueue()
    {
        //_queue = new BlockingCollection<T>();
        _queue = new SafeCollection<T>();
    }

    public async Task EnqueueAsync(T item, CancellationToken stoppingToken = default)
    {
        await _sem.WaitAsync(stoppingToken);

        if (!_queue.TryAdd(item))
        {
            Console.WriteLine($"Failed to enqueue item: {item}");
        }

        _sem.Release();
        await Task.CompletedTask;
    }

    public async Task EnqueueRangeAsync(IEnumerable<T> items, CancellationToken stoppingToken = default)
    {
        await _sem.WaitAsync(stoppingToken);

        _queue.AddRange(items);

        _sem.Release();
        await Task.CompletedTask;
    }

    public async Task<T?> DequeueAsync(CancellationToken stoppingToken = default)
    {
        await _sem.WaitAsync(stoppingToken);

        if (!_queue.TryTake(out T? item))
        {
            Console.WriteLine($"Failed to dequeue item: {item}");
        }

        _sem.Release();
        return item;
    }

    public async Task<IEnumerable<T>?> DequeueBulkAsync(uint maxBatchSize, CancellationToken stoppingToken = default)
    {
        if (!_queue.Any())
        {
            return null!;
        }

        //await _sem.WaitAsync(stoppingToken);

        //var result = new List<T>();
        //for (var i = 0; i < maxBatchSize; i++)
        //{
        //    if (!_queue.Any())
        //        break;

        //    if (!_queue.TryTake(out T? item))
        //    {
        //        Console.WriteLine($"Failed to dequeue item: {item}");
        //        continue;
        //    }

        //    result.Add(item);
        //}

        //var results = _queue.Take((int)maxBatchSize);
        //_sem.Release();
        var results = _queue.Take((int)maxBatchSize);
        return await Task.FromResult(results);

        //for (; ; )
        //{
        //    await _sem.WaitAsync(stoppingToken);

        //    if (_queue.IsEmpty)
        //    {
        //        _sem.Release();
        //        return null;
        //    }

        //    var result = new List<T>();
        //    T? item;
        //    for (var i = 0; i < maxBatchSize; i++)
        //    {
        //        if (_queue.TryDequeue(out item))
        //        {
        //            result.Add(item);
        //        }
        //    }

        //    _sem.Release();
        //    return result;
        //}
    }
}