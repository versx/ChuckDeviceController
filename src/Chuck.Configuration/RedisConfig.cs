namespace Chuck.Configuration
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Redis client configuration class.
    /// </summary>
    public class RedisConfig
    {
        /// <summary>
        /// Redis server address
        /// </summary>
        [JsonPropertyName("host")]
        public string Host { get; set; }

        /// <summary>
        /// Redis server listening port
        /// </summary>
        [JsonPropertyName("port")]
        public ushort Port { get; set; }

        /// <summary>
        /// Redis server secret/password
        /// </summary>
        [JsonPropertyName("password")]
        public string Password { get; set; }

        /// <summary>
        /// Redis database number
        /// </summary>
        [JsonPropertyName("databaseNum")]
        public int DatabaseNum { get; set; }

        /// <summary>
        /// Redis queue/list name to use
        /// </summary>
        [JsonPropertyName("queueName")]
        public string QueueName { get; set; }
    }
}