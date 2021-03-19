﻿namespace Chuck.Configuration
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Chuck.Extensions;

    public class Config
    {
        [JsonPropertyName("controllerInterface")]
        public string ControllerInterface { get; set; }

        [JsonPropertyName("controllerPort")]
        public ushort ControllerPort { get; set; }

        [JsonPropertyName("parserInterface")]
        public string ParserInterface { get; set; }

        [JsonPropertyName("parserPort")]
        public ushort ParserPort { get; set; }

        [JsonPropertyName("db")]
        public DatabaseConfig Database { get; set; }

        [JsonPropertyName("redis")]
        public RedisConfig Redis { get; set; }

        [JsonPropertyName("enableProfiler")]
        public bool EnableProfiler { get; set; }

        [JsonPropertyName("deviceAuth")]
        public DeviceAuthConfig DeviceAuth { get; set; }

        [JsonPropertyName("discord")]
        public DiscordAuthConfig Discord { get; set; }

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