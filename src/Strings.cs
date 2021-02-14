namespace ChuckDeviceController
{
    using System;
    using System.Globalization;

    public static class Strings
    {
        public const string DefaultConfigFileName = "config.json";
        public const string ViewsFolder = "Views";
        public const string TemplateExt = ".mustache";

        public static string Started {
            get
            {
                // Create a DateTime value.
                DateTime dtIn = DateTime.UtcNow.AddSeconds(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalSeconds);
                // Retrieve a CultureInfo object.
                CultureInfo invC = CultureInfo.InvariantCulture;
                return dtIn.ToString("r", invC);
            }
        }
    }
}