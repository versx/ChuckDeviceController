namespace ChuckDeviceController.Net.Models.Requests
{
    using System.Text.Json.Serialization;

    public class DevicePayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        /*
        [JsonPropertyName("min_level")]
        public int MinimumLevel { get; set; } = 0;

        [JsonPropertyName("max_level")]
        public int MaximumLevel { get; set; } = 29;
        */
    }
}