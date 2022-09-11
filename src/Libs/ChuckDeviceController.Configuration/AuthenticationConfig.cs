namespace ChuckDeviceController.Configuration
{
    public class AuthenticationConfig
    {
        public OpenAuthConfig Discord { get; set; } = new();

        public OpenAuthConfig GitHub { get; set; } = new();

        public OpenAuthConfig Google { get; set; } = new();
    }
}