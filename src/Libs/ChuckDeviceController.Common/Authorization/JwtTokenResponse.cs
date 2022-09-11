namespace ChuckDeviceController.Common.Authorization
{
    using System.Text.Json.Serialization;

    public class JwtTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public uint ExpiresIn { get; set; }

        [JsonPropertyName("status")]
        public JwtAuthorizationStatus Status { get; set; }

        public JwtTokenResponse(string accessToken, uint expiresIn, JwtAuthorizationStatus status)
        {
            AccessToken = accessToken;
            ExpiresIn = expiresIn;
            Status = status;
        }
    }
}