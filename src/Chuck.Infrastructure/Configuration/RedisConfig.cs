namespace Chuck.Infrastructure.Configuration
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// MySQL database configuration class.
    /// </summary>
    public class RedisConfig
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
        /// MySQL password
        /// </summary>
        [JsonPropertyName("password")]
        public string Password { get; set; }

        /// <summary>
        /// MySQL database name
        /// </summary>
        [JsonPropertyName("databaseNum")]
        public int DatabaseNum { get; set; }
    }
}