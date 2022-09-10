namespace ChuckDeviceController.Plugin.EventBus
{
    /// <summary>
    /// Indicates the result status of an observable emitted event.
    /// </summary>
    public enum EventExecutionResult
    {
        /// <summary>
        /// Event was executed successfully.
        /// </summary>
        Executed = 0,

        /// <summary>
        /// Unhandled exception occurred while emitting the event.
        /// </summary>
        UnhandledException = -1,
    }
}