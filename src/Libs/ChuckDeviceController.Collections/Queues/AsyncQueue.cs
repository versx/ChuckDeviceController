namespace ChuckDeviceController.Collections.Queues
{
    using System.Collections.Concurrent;

    public class AsyncQueue<T> : IAsyncQueue<T>
    {
        private readonly SemaphoreSlim _sem;
        private readonly ConcurrentQueue<T> _queue;

        public uint Count => Convert.ToUInt32(_queue?.Count ?? 0);

        public AsyncQueue()
        {
            _sem = new SemaphoreSlim(0);
            _queue = new ConcurrentQueue<T>();
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
            _sem.Release();
        }

        public void EnqueueRange(IEnumerable<T> items)
        {
            var index = 0;
            foreach (var item in items)
            {
                _queue.Enqueue(item);
                index++;
            }
            _sem.Release(index);
        }

        public async Task<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            for (; ; )
            {
                await _sem.WaitAsync(cancellationToken);

                if (_queue.TryDequeue(out T? item))
                {
                    return item;
                }
            }
        }

        public async Task<IEnumerable<T>> DequeueBulkAsync(uint maxBatchSize, CancellationToken cancellationToken = default)
        {
            for (; ; )
            {
                await _sem.WaitAsync(cancellationToken);

                var result = new List<T>();
                for (var i = 0; i < maxBatchSize; i++)
                {
                    if (_queue.IsEmpty)
                        break;

                    if (_queue.TryDequeue(out T? item))
                    {
                        result.Add(item);
                    }
                }
                return result;
            }
        }
    }
}