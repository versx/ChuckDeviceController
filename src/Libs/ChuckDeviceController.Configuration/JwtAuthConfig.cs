namespace ChuckDeviceController.Configuration
{
    public class JwtAuthConfig
    {
        public string Issuer { get; set; }

        public string Audience { get; set; }

        public string Key { get; set; }
    }
}