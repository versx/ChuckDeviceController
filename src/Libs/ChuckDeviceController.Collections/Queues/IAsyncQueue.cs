namespace ChuckDeviceController.Collections.Queues
{
    public interface IAsyncQueue<T>
    {
        uint Count { get; }


        Task EnqueueAsync(T item);

        Task EnqueueRangeAsync(IEnumerable<T> items);

        Task<T?> DequeueAsync(CancellationToken cancellationToken);

        Task<IEnumerable<T>> DequeueBulkAsync(
            uint maxBatchSize,
            CancellationToken cancellationToken = default);
    }
}