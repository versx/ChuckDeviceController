namespace ChuckDeviceController.Configuration
{
    public class WebhookRelayConfig
    {
        public ushort MaximumRetryCount { get; set; } = 3;

        public ushort RequestTimeout { get; set; } = 15;
    }
}