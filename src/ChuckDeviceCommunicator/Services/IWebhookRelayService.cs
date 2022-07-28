namespace ChuckDeviceCommunicator.Services
{
    using ChuckDeviceController.Protos;

    public interface IWebhookRelayService
    {
        void Start();

        void Stop();

        void Reload();

        void Enqueue(WebhookPayloadType webhookType, string json);
    }
}