namespace Chuck.Infrastructure.Extensions
{
    using Microsoft.Extensions.Logging;

    public class ConsoleExt
    {
        private static readonly ILogger<ConsoleExt> _logger = new Logger<ConsoleExt>(LoggerFactory.Create(x => x.AddConsole()));

        public static void WriteInfo(string format, params object[] args)
        {
            //WriteLine(ConsoleColor.White, $"info: {format}", args);
            _logger.LogInformation(format, args);
        }

        public static void WriteDebug(string format, params object[] args)
        {
            //WriteLine(ConsoleColor.Gray, $"dbug: {format}", args);
            _logger.LogDebug(format, args);
        }

        public static void WriteWarn(string format, params object[] args)
        {
            //WriteLine(ConsoleColor.Yellow, $"warn: {format}", args);
            _logger.LogWarning(format, args);
        }

        public static void WriteError(string format, params object[] args)
        {
            //WriteLine(ConsoleColor.Red, $"fail: {format}", args);
            _logger.LogError(format, args);
        }

        /*
        public static void WriteError(Exception exception)
        {
            WriteLine(ConsoleColor.Red, $"Error: {exception}");
        }

        private static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            var message = args.Length > 0
                ? string.Format(format, args)
                : format;
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }
        */
    }
}