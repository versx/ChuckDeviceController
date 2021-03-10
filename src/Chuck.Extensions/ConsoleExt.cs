namespace Chuck.Extensions
{
    using System;

    public static class ConsoleExt
    {
        public static void WriteInfo(string format, params object[] args)
        {
            WriteLine(ConsoleColor.White, $"info: {format}", args);
        }

        public static void WriteDebug(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Gray, $"dbug: {format}", args);
        }

        public static void WriteWarn(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Yellow, $"warn: {format}", args);
        }

        public static void WriteError(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Red, $"fail: {format}", args);
        }

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
    }
}