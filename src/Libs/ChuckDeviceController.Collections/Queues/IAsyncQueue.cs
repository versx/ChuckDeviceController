namespace ChuckDeviceController.Collections.Queues
{
    public interface IAsyncQueue<T>
    {
        uint Count { get; }


        Task EnqueueAsync(T item, CancellationToken stoppingToken = default);

        Task EnqueueRangeAsync(IEnumerable<T> items, CancellationToken stoppingToken = default);

        Task<T?> DequeueAsync(CancellationToken stoppingToken = default);

        Task<IEnumerable<T>> DequeueBulkAsync(
            uint maxBatchSize,
            CancellationToken stoppingToken = default);
    }
}