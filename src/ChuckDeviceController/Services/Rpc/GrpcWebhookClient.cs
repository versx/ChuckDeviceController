namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public class GrpcWebhookClient : IGrpcClient<WebhookPayload.WebhookPayloadClient, WebhookPayloadRequest, WebhookPayloadResponse>
    {
        private readonly WebhookPayload.WebhookPayloadClient _client;

        public GrpcWebhookClient(WebhookPayload.WebhookPayloadClient client)
        {
            _client = client;
        }

        public async Task<WebhookPayloadResponse?> SendAsync(WebhookPayloadRequest payload)
        {
            // TODO: Add config property deciding whether to enable webhooks or not
            try
            {
                var response = await _client.ReceivedWebhookPayloadAsync(payload);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendAsync] Error: {ex.Message}");
            }
            return null;
        }
    }
}