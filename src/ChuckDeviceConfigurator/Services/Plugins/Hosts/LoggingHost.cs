namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Plugin;

    public class LoggingHost : ILoggingHost
    {
        private readonly ILogger<ILoggingHost> _logger;

        public LoggingHost()
        {
            _logger = new Logger<ILoggingHost>(LoggerFactory.Create(x => x.AddConsole()));
        }

        public void LogTrace(string message, params object?[] args)
        {
            _logger.LogTrace(message, args);
        }

        public void LogInformation(string message, params object?[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogDebug(string message, params object?[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void LogWarning(string message, params object?[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(string message, params object?[] args)
        {
            _logger.LogError(message, args);
        }

        public void LogError(Exception error, string? message = null, params object?[] args)
        {
            _logger.LogError(error, message, args);
        }

        public void LogCritical(string message, params object?[] args)
        {
            _logger.LogError(message, args);
        }

        public void LogCritical(Exception error, string? message = null, params object?[] args)
        {
            _logger.LogError(error, message, args);
        }
    }
}