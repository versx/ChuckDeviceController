namespace ChuckDeviceController.Collections.Queues;

using System.Collections.Concurrent;

public class DefaultBackgroundTaskQueue<T> : IBackgroundTaskQueue<T> where T : class
{
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public uint Count => Convert.ToUInt32(_queue.Count);

    public DefaultBackgroundTaskQueue(int capacity = 4096)
    {
    }

    public async Task EnqueueAsync(Func<CancellationToken, Task> workItem)
    {
        if (workItem is null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        _queue.Enqueue(workItem);
        _signal.Release();

        await Task.CompletedTask;
    }

    public async Task<Func<CancellationToken, Task>?> DequeueAsync(
        CancellationToken cancellationToken)
    {
        if (Count == 0)
        {
            return null;
        }

        await _signal.WaitAsync(cancellationToken);

        _queue.TryDequeue(out var workItem);
        return workItem;
    }

    public async Task<List<Func<CancellationToken, Task>>?> DequeueMultipleAsync(
        int maxBatchSize,
        CancellationToken cancellationToken)
    {
        if (Count == 0)
        {
            return null;
        }

        await _signal.WaitAsync(cancellationToken);

        var workItems = new List<Func<CancellationToken, Task>>();
        for (var i = 0; i < maxBatchSize; i++)
        {
            if (_queue.IsEmpty)
                break;

            _queue.TryDequeue(out var workItem);
            if (workItem is not null)
            {
                workItems.Add(workItem);
            }
        }

        return workItems;
    }
}