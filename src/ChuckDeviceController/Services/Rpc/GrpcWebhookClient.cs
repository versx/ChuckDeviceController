namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public interface IGrpcWebhookClient
    {
        Task<WebhookPayloadResponse?> SendWebhookPayloadAsync(WebhookPayloadType webhookType, string json);
    }

    public class GrpcWebhookClient : IGrpcWebhookClient
    {
        private readonly WebhookPayload.WebhookPayloadClient _client;

        public GrpcWebhookClient(WebhookPayload.WebhookPayloadClient client)
        {
            _client = client;
        }

        public async Task<WebhookPayloadResponse?> SendWebhookPayloadAsync(WebhookPayloadType webhookType, string json)
        {
            // Create gRPC payload request
            var request = new WebhookPayloadRequest
            {
                PayloadType = webhookType,
                Payload = json,
            };

            // Handle the response of the request
            var response = await _client.ReceivedWebhookPayloadAsync(request);
            return response;
        }
    }
}