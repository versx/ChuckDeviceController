namespace ChuckDeviceController.Configuration
{
    public class JwtAuthConfig
    {
        public bool Enabled { get; set; }

        public uint TokenValidityM { get; set; } = 43200; // 30 days in minutes

        public string Issuer { get; set; } = null!;

        public string Audience { get; set; } = null!;

        public string Key { get; set; } = null!;
    }
}