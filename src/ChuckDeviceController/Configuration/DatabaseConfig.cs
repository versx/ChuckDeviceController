/*
namespace ChuckDeviceController.Configuration
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// MySQL database configuration class.
    /// </summary>
    public class DatabaseConfig
    {
        /// <summary>
        /// MySQL host address
        /// </summary>
        [JsonPropertyName("host")]
        public string Host { get; set; }

        /// <summary>
        /// MySQL listening port
        /// </summary>
        [JsonPropertyName("port")]
        public ushort Port { get; set; }

        /// <summary>
        /// MySQL username
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; }

        /// <summary>
        /// MySQL password
        /// </summary>
        [JsonPropertyName("password")]
        public string Password { get; set; }

        /// <summary>
        /// MySQL database name
        /// </summary>
        [JsonPropertyName("database")]
        public string Database { get; set; }

        /// <summary>
        /// Returns the string representation of the <see cref="DatabaseConfig"/> class.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Uid={Username};");
            sb.Append($"Password={Password};");
            sb.Append($"Host={Host};");
            sb.Append($"Port={Port};");
            sb.Append($"Database={Database};");
            sb.Append("old guids=true;");
            sb.Append("Allow User Variables=true;");
            var connectionString = sb.ToString();
            return connectionString;
        }
    }
}
*/