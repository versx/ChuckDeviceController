namespace ChuckDeviceController
{
    using System;
    using System.Globalization;

    public static class Strings
    {
        public const string DefaultConfigFileName = "config.json";
        public static readonly string WebRoot = $"{AppDomain.CurrentDomain.BaseDirectory}/wwwroot";
        public static readonly string ViewsFolder = WebRoot +"/Views";
        public const string TemplateExt = ".mustache";
        public static readonly string DataFolder = WebRoot + "/static/data";
        public static readonly string MigrationsFolder = WebRoot + "/migrations";
        public const string SQL_CREATE_TABLE_METADATA = @"
        CREATE TABLE IF NOT EXISTS metadata (
            `key` VARCHAR(50) PRIMARY KEY NOT NULL,
            `value` VARCHAR(50) DEFAULT NULL
        );";

        public static string Started
        {
            get
            {
                // Create a DateTime value.
                var dtIn = DateTime.UtcNow.AddSeconds(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalSeconds);
                // Retrieve a CultureInfo object.
                var invC = CultureInfo.InvariantCulture;
                return dtIn.ToString("r", invC);
            }
        }
    }
}