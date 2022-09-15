namespace ChuckDeviceController.Collections.Queues
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBackgroundTaskQueue<T>
    {
        /// <summary>
        /// Gets a value determining the length of the background task item queue
        /// </summary>
        uint Count { get; }

        /// <summary>
        /// Schedules a task which needs to be processed.
        /// </summary>
        /// <param name="workItem">Task item to be executed</param>
        /// <returns></returns>
        Task EnqueueAsync(Func<CancellationToken, Task> workItem);

        /// <summary>
        /// Attempts to remove and return the object at the beginning of the queue.
        /// </summary>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns></returns>
        Task<Func<CancellationToken, Task>?> DequeueAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxBatchSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<Func<CancellationToken, Task>>?> DequeueMultipleAsync(
            int maxBatchSize,
            CancellationToken cancellationToken);
    }
}