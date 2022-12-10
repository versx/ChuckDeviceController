namespace ChuckDeviceController.Logging
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;

    public static class GenericLoggerFactory
    {
        public static Logger<T> CreateLogger<T>(LogLevel logLevel = LogLevel.Information, bool addConsole = true)
        {
            var factory = LoggerFactory.Create(options =>
            {
                options.SetMinimumLevel(logLevel);
                if (addConsole)
                {
                    //options.AddConsole(x => });
                    options.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = false;
                        options.ColorBehavior = LoggerColorBehavior.Enabled;
                    });
                }
                options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                //options.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Error);
                options.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None);
                options.AddFilter("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware", LogLevel.None);
            });
            return new Logger<T>(factory);
        }
    }
}