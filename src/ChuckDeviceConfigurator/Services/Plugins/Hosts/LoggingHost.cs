namespace ChuckDeviceConfigurator.Services.Plugins.Hosts;

using ChuckDeviceController.Logging;
using ChuckDeviceController.Plugin;

public class LoggingHost : ILoggingHost
{
    private static readonly ILogger<ILoggingHost> _logger =
        GenericLoggerFactory.CreateLogger<ILoggingHost>();

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