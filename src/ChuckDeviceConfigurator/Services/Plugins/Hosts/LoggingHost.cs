namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Plugins;

    public class LoggingHost : ILoggingHost
    {
        private readonly ILogger<ILoggingHost> _logger;

        public LoggingHost(ILogger<ILoggingHost> logger)
        {
            _logger = logger;
        }

        public void LogException(Exception ex)
        {
            _logger.LogError($"Exception occurred from plugin: {ex}");
        }

        public void LogMessage(string text, params object[] args)
        {
            var message = string.Format(text, args);
            _logger.LogInformation($"Message from plugin: {message}");
        }
    }
}