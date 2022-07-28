namespace ChuckDeviceCommunicator.Services
{
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Protos;

    public interface IWebhookRelayService
    {
        bool IsRunning { get; }

        ulong TotalSent { get; }

        IEnumerable<Webhook> WebhookEndpoints { get; }


        void Start();

        void Stop();

        void Reload();

        void Enqueue(WebhookPayloadType webhookType, string json);
    }
}