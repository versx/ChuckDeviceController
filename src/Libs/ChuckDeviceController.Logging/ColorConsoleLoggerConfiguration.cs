namespace ChuckDeviceController.Logging;

using Microsoft.Extensions.Logging;

public sealed class ColorConsoleLoggerConfiguration
{
    public int EventId { get; set; }

    public Dictionary<LogLevel, ConsoleColor> LogLevelColorMap { get; set; } = new()
    {
        [LogLevel.Trace] = ConsoleColor.Cyan,
        [LogLevel.Information] = ConsoleColor.White,
        [LogLevel.Debug] = ConsoleColor.DarkGray,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Critical] = ConsoleColor.DarkRed,
    };

    public bool UseTimestamp { get; set; } = true;

    public bool UseUnix { get; set; } = false;

    public string TimestampFormat { get; set; } = "{0:HH}:{0:mm}:{0:ss}";
}