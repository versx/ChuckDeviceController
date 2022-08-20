namespace ChuckDeviceConfigurator.Configuration
{
    public class AuthenticationConfig
    {
        public OpenAuthConfig Discord { get; set; } = new();

        public OpenAuthConfig GitHub { get; set; } = new();

        public OpenAuthConfig Google { get; set; } = new();
    }

    public class OpenAuthConfig
    {
        public bool Enabled { get; set; }

        public string? ClientId { get; set; }

        public string? ClientSecret { get; set; }
    }
}