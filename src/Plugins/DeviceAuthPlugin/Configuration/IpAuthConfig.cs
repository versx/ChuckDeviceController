namespace DeviceAuthPlugin.Configuration
{
    public class IpAuthConfig
    {
        public bool Enabled { get; set; }

        public List<string> IpAddresses { get; set; } = new();

        // TODO: Affected routes list
    }
}