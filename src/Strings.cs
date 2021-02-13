namespace ChuckDeviceController
{
    using System;

    public static class Strings
    {
        public const string DefaultConfigFileName = "config.json";

        public static DateTime Started { get; } = DateTime.UtcNow;
    }
}