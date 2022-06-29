/*
namespace ChuckDeviceController.Configuration
{
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Extensions;

    public class Config
    {
        /// <summary>
        /// Gets or sets the HTTP listening interface/host address
        /// </summary>
        [JsonPropertyName("host")]
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the HTTP listening port
        /// </summary>
        [JsonPropertyName("port")]
        public ushort Port { get; set; }

        /// <summary>
        /// Gets or sets the Database configuration
        /// </summary>
        [JsonPropertyName("database")]
        public DatabaseConfig Database { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to log incoming webhook data to a file
        /// </summary>
        [JsonPropertyName("debug")]
        public bool Debug { get; set; }

        /// <summary>
        /// Gets or sets the event logging level to set
        /// </summary>
        [JsonPropertyName("logLevel")]
        public LogLevel LogLevel { get; set; }
        //
         * Trace: 0
         * Debug: 1
         * Info: 2
         * Warning: 3
         * Error: 4
         * Critical: 5
         * None: 6
         //

        /// <summary>
        /// Gets or sets the configuration file path
        /// </summary>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="Config"/> class
        /// </summary>
        public Config()
        {
            Host = "*";
            Port = 8888;
            LogLevel = LogLevel.Trace;
        }

        /// <summary>
        /// Save the current configuration object
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        public void Save(string filePath)
        {
            var data = this.ToJson();
            File.WriteAllText(filePath, data);
        }

        /// <summary>
        /// Load the configuration from a file
        /// </summary>
        /// <param name="filePath">Path to load the configuration file from</param>
        /// <returns>Returns the deserialized configuration object</returns>
        public static Config Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Config not loaded because file not found.", filePath);
            }
            var config = filePath.LoadFromFile<Config>();
            return config;
        }
    }
}
*/