namespace ChuckDeviceCommunicator.Configuration
{
    public class WebhookRelayConfig
    {
        public ushort MaximumRetryCount { get; set; }

        public ushort RequestTimeout { get; set; }
    }
}