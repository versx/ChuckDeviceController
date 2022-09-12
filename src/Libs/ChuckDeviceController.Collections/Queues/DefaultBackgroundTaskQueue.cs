namespace ChuckDeviceController.Collections.Queues
{
    using System.Threading.Channels;

    using ChuckDeviceController.Collections.Extensions;

    public class DefaultBackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

        public uint Count => Convert.ToUInt32(_queue?.Reader?.Count ?? 0);

        public DefaultBackgroundTaskQueue(int capacity = 4096)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,//Wait,
                Capacity = capacity,
            };
            _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        }

        public async ValueTask EnqueueAsync(Func<CancellationToken, ValueTask> workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }
            await _queue.Writer.WriteAsync(workItem);
        }

        public async Task<Func<CancellationToken, ValueTask>> DequeueAsync(
            CancellationToken cancellationToken)
        {
            if (Count == 0)
            {
                return new Func<CancellationToken, ValueTask>(token => new());
            }

            var workItem = await _queue.Reader.ReadAsync(cancellationToken);
            return workItem;
        }

        public async Task<List<Func<CancellationToken, ValueTask>>> DequeueMultipleAsync(
            int maxBatchSize,
            CancellationToken cancellationToken)
        {
            if (Count == 0)
            {
                return new();
            }

            var workItems = await _queue.Reader.ReadMultipleAsync(maxBatchSize, cancellationToken);
            return workItems;
        }
    }
}