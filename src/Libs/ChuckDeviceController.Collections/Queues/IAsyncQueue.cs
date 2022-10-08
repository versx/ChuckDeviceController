namespace ChuckDeviceController.Collections.Queues
{
    public interface IAsyncQueue<T>
    {
        uint Count { get; }


        void Enqueue(T item);

        void EnqueueRange(IEnumerable<T> items);

        Task<T> DequeueAsync(CancellationToken cancellationToken);

        Task<IEnumerable<T>> DequeueBulkAsync(
            uint maxBatchSize,
            CancellationToken cancellationToken = default);
    }
}