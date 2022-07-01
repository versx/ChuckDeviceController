namespace ChuckDeviceConfigurator.Net.Models.Responses
{
    using System.Text.Json.Serialization;

    public class DeviceAccountResponse
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("first_warning_timestamp")]
        public ulong? FirstWarningTimestamp { get; set; }

        [JsonPropertyName("level")]
        public ushort Level { get; set; }
    }
}