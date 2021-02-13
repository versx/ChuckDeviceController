namespace ChuckDeviceController
{
    using System;

    public static class Strings
    {
        private static readonly DateTime _started = DateTime.UtcNow;

        public const string DefaultConfigFileName = "config.json";

        public static DateTime Started => _started;
    }
}