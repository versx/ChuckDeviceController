namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Plugin;

    public class LoggingHost : ILoggingHost
    {
        private readonly ILogger<ILoggingHost> _logger;

        public LoggingHost(ILogger<ILoggingHost> logger)
        {
            _logger = logger;
        }

        public void LogException(Exception ex)
        {
            if (_logger == null)
                return;

            _logger.LogError($"----------------------- Exception occurred from plugin: {ex}");
        }

        public void LogMessage(string text, params object[] args)
        {
            if (_logger == null)
                return;

            var message = args.Length > 0
                ? string.Format(text, args)
                : text;
            _logger.LogInformation($"----------------------- Message from plugin: {message}");
        }
    }
}