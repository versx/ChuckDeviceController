namespace ChuckDeviceController.Configuration
{
    public class JwtAuthConfig
    {
        public bool Enabled { get; set; }

        public uint TokenValidityM { get; set; } = 30; // minutes

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public string Key { get; set; }
    }
}