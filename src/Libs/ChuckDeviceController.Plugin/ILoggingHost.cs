namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Plugin host handler for logging messages from plugins.
    /// </summary>
    public interface ILoggingHost
    {
        void LogTrace(string message, params object?[] args);

        void LogInformation(string message, params object?[] args);

        void LogDebug(string message, params object?[] args);

        void LogWarning(string message, params object?[] args);

        void LogError(string message, params object?[] args);

        void LogError(Exception error, string? message = null, params object?[] args);

        void LogCritical(string message, params object?[] args);

        void LogCritical(Exception error, string? message = null, params object?[] args);
    }
}