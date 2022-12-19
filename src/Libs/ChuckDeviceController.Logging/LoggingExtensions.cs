namespace ChuckDeviceController.Logging;

using Microsoft.Extensions.Logging;

public static class LoggingExtensions
{
    public static ILoggingBuilder GetLoggingConfig(this ILoggingBuilder configure, LogLevel defaultLogLevel = LogLevel.Information)
    {
        configure.SetMinimumLevel(defaultLogLevel);
        //configure.AddSimpleConsole(options =>
        //{
        //    options.IncludeScopes = false;
        //    options.ColorBehavior = LoggerColorBehavior.Enabled;
        //});
        configure.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        //configure.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Error);
        configure.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None);
        configure.AddFilter("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware", LogLevel.None);

        return configure;
    }
}