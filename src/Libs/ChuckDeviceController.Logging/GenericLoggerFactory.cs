namespace ChuckDeviceController.Logging;

using Microsoft.Extensions.Logging;

public static class GenericLoggerFactory
{
    public static Logger<T> CreateLogger<T>(LogLevel logLevel = LogLevel.Information)
    {
        var factory = LoggerFactory.Create(options =>
        {
            options.GetLoggingConfig(logLevel);
            options.AddColorConsoleLogger();
        });
        return new Logger<T>(factory);
    }
}