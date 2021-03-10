namespace ChuckDeviceController.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json.Serialization;

    using Chuck.Extensions;

    public class TimeZoneService
    {
        private const string TimezonesFileName = "timezones.json";

        #region Singleton

        private static TimeZoneService _instance;
        public static TimeZoneService Instance =>
            _instance ??= new TimeZoneService();

        #endregion

        [JsonIgnore]
        public IReadOnlyDictionary<string, TimezoneOffsetData> Timezones { get; set; }

        public TimeZoneService()
        {
            Timezones = LoadInit<Dictionary<string, TimezoneOffsetData>>(
                Path.Combine(
                    Strings.DataFolder,
                    TimezonesFileName
                )
            );
        }

        private static T LoadInit<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                ConsoleExt.WriteError($"File {filePath} is empty.");
                return default;
            }

            return data.FromJson<T>();
        }
    }

    public class TimezoneOffsetData
    {
        [JsonPropertyName("utc")]
        public short Utc { get; set; }

        [JsonPropertyName("dst")]
        public short Dst { get; set; }
    }
}