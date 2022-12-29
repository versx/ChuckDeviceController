namespace ChuckDeviceController.Logging;

using Microsoft.Extensions.Logging;

public sealed class ColorConsoleLogger : ILogger
{
    private readonly string _name;
    private readonly Func<ColorConsoleLoggerConfiguration> _getCurrentConfig;

    public ColorConsoleLogger(
        string name,
        Func<ColorConsoleLoggerConfiguration> getCurrentConfig)
    {
        var className = name.Split('.').LastOrDefault() ?? name;
        _name = className;
        _getCurrentConfig = getCurrentConfig;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) =>
        _getCurrentConfig().LogLevelColorMap.ContainsKey(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var config = _getCurrentConfig();
        if (config.EventId == 0 || config.EventId == eventId.Id)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            if (config.UseTimestamp && !string.IsNullOrEmpty(config.TimestampFormat))
            {
                var time = config.UseUnix ? DateTime.UtcNow : DateTime.Now;
                var timestamp = string.Format(config.TimestampFormat, time);
                Console.Write($"[{timestamp}] ");
            }

            Console.ForegroundColor = config.LogLevelColorMap[logLevel];
            //Console.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");
            //Console.WriteLine($"[{eventId.Id}:{logLevel,-11}]");
            Console.Write($"[{eventId.Id}:{logLevel,-11}]");

            Console.ForegroundColor = originalColor;
            //Console.Write($"     {_name} - ");
            Console.Write($" [{_name}] ");

            Console.ForegroundColor = config.LogLevelColorMap[logLevel];
            Console.Write($"{formatter(state, exception)}");

            Console.ForegroundColor = originalColor;
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}