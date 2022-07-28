namespace ChuckDeviceCommunicator.Services
{
    using ChuckDeviceController.Protos;

    public interface IWebhookEndpoint
    {
        bool Enabled { get; }

        string Url { get; }

        double Delay { get; }

        IEnumerable<WebhookPayloadType> AllowedTypes { get; }
    }
}