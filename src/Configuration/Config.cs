namespace ChuckDeviceController.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Extensions;

    public class Config
    {
        [JsonPropertyName("interface")]
        public string Interface { get; set; }

        [JsonPropertyName("port")]
        public ushort Port { get; set; }

        [JsonPropertyName("timezoneOffset")]
        public short TimezoneOffset { get; set; } = (short)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalSeconds;

        [JsonPropertyName("db")]
        public DatabaseConfig Database { get; set; }

        [JsonPropertyName("webhooks")]
        public IReadOnlyList<string> Webhooks { get; set; }

        /// <summary>
        /// Save the current configuration object
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        public void Save(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
            var data = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, data);
        }

        /// <summary>
        /// Load the configuration from a file
        /// </summary>
        /// <param name="filePath">Path to load the configuration file from</param>
        /// <returns>Returns the deserialized configuration object</returns>
        public static Config Load(string filePath)
        {
            return filePath.LoadFile<Config>();
        }
    }
}