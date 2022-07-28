namespace ChuckDeviceCommunicator.Services
{
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Protos;

    public interface IWebhookRelayService
    {
        bool IsRunning { get; }

        IEnumerable<Webhook> WebhookEndpoints { get; }

        ulong TotalSent { get; }


        void Start();

        void Stop();

        void Reload();

        void Enqueue(WebhookPayloadType webhookType, string json);
    }
}