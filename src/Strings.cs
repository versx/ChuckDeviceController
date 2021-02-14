namespace ChuckDeviceController
{
    using System;

    public static class Strings
    {
        public const string DefaultConfigFileName = "config.json";
        public const string ViewsFolder = "Views";
        public const string TemplateExt = ".mustache";

        public static DateTime Started { get; } = DateTime.UtcNow;
    }
}