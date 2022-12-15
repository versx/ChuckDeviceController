namespace ChuckDeviceConfigurator.Services.Jobs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IQueueManager
    {
        /// <summary>
        /// Gets the Quest or Pokemon IV queue by instance name.
        /// *(Must be Quest or Pokemon IV job controller instance)*
        /// </summary>
        /// <typeparam name="T">Return type for queue items.</typeparam>
        /// <param name="instanceName">Name of the instance to get the queue from.</param>
        /// <returns>Returns a read only list of pending queued Pokestops or Pokemon.</returns>
        IReadOnlyList<T> GetQueue<T>(string instanceName);

        /// <summary>
        /// Removes a queued Pokestop or Pokemon from the specified queue by
        /// Pokestop ID or Pokemon encounter ID.
        /// </summary>
        /// <param name="instanceName">Name of instance with queue.</param>
        /// <param name="id">Pokestop ID or Pokemon encounter ID to remove.</param>
        void RemoveFromQueue(string instanceName, string id);

        /// <summary>
        /// Clears all pending Pokestops or Pokemon encounters from the specified job controller instance queue.
        /// </summary>
        /// <param name="instanceName">Name of the instance queue to clear.</param>
        void ClearQueue(string instanceName);
    }
}