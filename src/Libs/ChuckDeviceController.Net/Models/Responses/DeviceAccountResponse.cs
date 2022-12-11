namespace ChuckDeviceController.Net.Models.Responses
{
    using System.Text.Json.Serialization;

    public class DeviceAccountResponse
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = null!;

        [JsonPropertyName("password")]
        public string Password { get; set; } = null!;

        [JsonPropertyName("first_warning_timestamp")]
        public ulong? FirstWarningTimestamp { get; set; }

        [JsonPropertyName("level")]
        public ushort Level { get; set; }
    }
}