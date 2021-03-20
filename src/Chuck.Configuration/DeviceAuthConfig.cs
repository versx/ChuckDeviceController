namespace Chuck.Configuration
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class DeviceAuthConfig
    {
        [JsonPropertyName("allowedHosts")]
        public IReadOnlyList<string> AllowedHosts { get; set; }

        [JsonPropertyName("allowedTokens")]
        public IReadOnlyList<string> AllowedTokens { get; set; }

        public DeviceAuthConfig()
        {
            AllowedHosts = new List<string>();
            AllowedTokens = new List<string>();
        }
    }
}