namespace ChuckDeviceController.Collections.Queues
{
    using System.Collections.Concurrent;

    public class AsyncQueue<T> : IAsyncQueue<T>
    {
        private readonly SemaphoreSlim _sem = new(1, 1);
        private readonly ConcurrentQueue<T> _queue;

        public uint Count => Convert.ToUInt32(_queue?.Count ?? 0);

        public AsyncQueue()
        {
            _queue = new ConcurrentQueue<T>();
        }

        public async Task EnqueueAsync(T item)
        {
            await _sem.WaitAsync();
            _queue.Enqueue(item);
            _sem.Release();
        }

        public async Task EnqueueRangeAsync(IEnumerable<T> items)
        {
            await _sem.WaitAsync();
            //var index = 0;
            foreach (var item in items)
            {
                _queue.Enqueue(item);
                //index++;
            }
            //_sem.Release(index);
            _sem.Release();
        }

        public async Task<T?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            await _sem.WaitAsync(cancellationToken);
            for (; ; )
            {
                if (!_queue.TryDequeue(out T? item))
                {
                    Console.WriteLine($"Failed to dequeue item");
                    _sem.Release();
                    continue;
                }
                _sem.Release();
                return item;
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

                _sem.Release();
                return result;
            }
        }
    }
}