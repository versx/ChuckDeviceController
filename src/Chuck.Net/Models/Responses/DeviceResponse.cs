namespace Chuck.Net.Models.Responses
{
    using System.Text.Json.Serialization;

    public class DeviceResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public dynamic Data { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }
    }

    public class DeviceAssignmentResponse
    {
        [JsonPropertyName("assigned")]
        public bool Assigned { get; set; }

        [JsonPropertyName("first_warning_timestamp")]
        public ulong? FirstWarningTimestamp { get; set; }
    }

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