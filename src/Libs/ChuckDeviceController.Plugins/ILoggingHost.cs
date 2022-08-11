namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// Plugin host handler for logging messages from plugins.
    /// </summary>
    public interface ILoggingHost
    {
        /// <summary>
        /// Log a message to the host application.
        /// </summary>
        /// <param name="text">Formatted log message string.</param>
        /// <param name="args">Arguments to parse with log message.</param>
        void LogMessage(string text, params object[] args);

        /// <summary>
        /// Log an exception that has been thrown to the
        /// host application.
        /// </summary>
        /// <param name="ex">Exception that was thrown.</param>
        void LogException(Exception ex);
    }
}