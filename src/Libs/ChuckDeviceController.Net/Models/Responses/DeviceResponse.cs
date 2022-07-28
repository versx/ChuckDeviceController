namespace ChuckDeviceController.Net.Models.Responses
{
    using System.Text.Json.Serialization;

    public class DeviceResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public dynamic? Data { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}