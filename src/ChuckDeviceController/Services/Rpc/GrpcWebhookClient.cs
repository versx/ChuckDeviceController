namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public class GrpcWebhookClient : IGrpcClient<WebhookPayload.WebhookPayloadClient, WebhookPayloadRequest, WebhookPayloadResponse>
    {
        private readonly ILogger<GrpcWebhookClient> _logger;
        private readonly WebhookPayload.WebhookPayloadClient _client;

        public GrpcWebhookClient(
            ILogger<GrpcWebhookClient> logger,
            WebhookPayload.WebhookPayloadClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<WebhookPayloadResponse?> SendAsync(WebhookPayloadRequest payload)
        {
            // TODO: Add config property deciding whether to enable webhooks or not
            try
            {
                var response = await _client.HandleWebhookPayloadAsync(payload);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
            return null;
        }
    }
}