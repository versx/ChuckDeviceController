namespace ChuckDeviceController.Authorization.Jwt.Models
{
    using System.Text.Json.Serialization;

    public class JwtResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = null!;

        [JsonPropertyName("expires_at")]
        public ulong ExpiresAt { get; set; }

        [JsonPropertyName("status")]
        public JwtStatus Status { get; set; }
    }
}