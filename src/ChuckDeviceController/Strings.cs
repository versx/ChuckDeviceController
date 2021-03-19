namespace ChuckDeviceController
{
    using System;
    using System.Globalization;

    public static class Strings
    {
        public const string AppName = "ChuckDeviceController";
        public const string DefaultConfigFileName = "config.json";
        public const string ViewsFolder = "Views";
        public const string TemplateExt = ".mustache";

        public const string WebRoot = "wwwroot";
        public const string DataFolder = WebRoot + "/static/data";
#if DEBUG
        public const string MigrationsFolder = "../../migrations";
#else
        public const string MigrationsFolder = "../migrations";
#endif

        public const string SQL_CREATE_TABLE_METADATA = @"
        CREATE TABLE IF NOT EXISTS metadata (
            `key` VARCHAR(50) PRIMARY KEY NOT NULL,
            `value` VARCHAR(50) DEFAULT NULL
        );";
        public const string All = "All";

        private static DateTime _started;
        public static string Started
        {
            get
            {
                if (_started == default)
                {
                    // Create a DateTime value.
                    _started = DateTime.UtcNow.AddSeconds(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalSeconds);
                }
                // Retrieve a CultureInfo object.
                var invC = CultureInfo.InvariantCulture;
                return _started.ToString("r", invC);
            }
        }
    }
}
