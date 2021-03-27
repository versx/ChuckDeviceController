namespace Chuck.Configuration
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class DiscordAuthConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("clientId")]
        public ulong ClientId { get; set; }

        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; }

        [JsonPropertyName("redirectUri")]
        public string RedirectUri { get; set; }

        [JsonPropertyName("userIds")]
        public IReadOnlyList<ulong> UserIDs { get; set; }

        public DiscordAuthConfig()
        {
            UserIDs = new List<ulong>();
        }
    }
}