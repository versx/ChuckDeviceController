namespace ChuckDeviceCommunicator.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public class GrpcWebhookEndpointsClient : IGrpcClient<WebhookEndpoint.WebhookEndpointClient, WebhookEndpointRequest, WebhookEndpointResponse>
    {
        private readonly ILogger<GrpcWebhookEndpointsClient> _logger;
        private readonly WebhookEndpoint.WebhookEndpointClient _client;

        public GrpcWebhookEndpointsClient(
            ILogger<GrpcWebhookEndpointsClient> logger,
            WebhookEndpoint.WebhookEndpointClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<WebhookEndpointResponse?> SendAsync(WebhookEndpointRequest payload)
        {
            try
            {
                var response = await _client.HandleWebhookEndpointAsync(payload);
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
