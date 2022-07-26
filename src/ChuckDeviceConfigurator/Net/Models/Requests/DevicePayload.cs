namespace ChuckDeviceConfigurator.Net.Models.Requests
{
    using System.Text.Json.Serialization;

    public class DevicePayload
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("uuid")]
        public string? Uuid { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }
}