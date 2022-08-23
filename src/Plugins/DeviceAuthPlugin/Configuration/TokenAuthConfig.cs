namespace DeviceAuthPlugin.Configuration
{
    public class TokenAuthConfig
    {
        public bool Enabled { get; set; }

        public List<string> Tokens { get; set; } = new();

        // TODO: Affected routes list
    }
}