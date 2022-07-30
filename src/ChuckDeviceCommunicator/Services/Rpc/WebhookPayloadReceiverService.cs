namespace ChuckDeviceCommunicator.Services.Rpc
{
    using Grpc.Core;

    using ChuckDeviceController.Protos;

    public class WebhookPayloadReceiverService : WebhookPayload.WebhookPayloadBase
    {
        private readonly ILogger<WebhookPayloadReceiverService> _logger;
        private readonly IWebhookRelayService _webhookRelayService;

        public WebhookPayloadReceiverService(
            ILogger<WebhookPayloadReceiverService> logger,
            IWebhookRelayService webhookRelayService)
        {
            _logger = logger;
            _webhookRelayService = webhookRelayService;
        }

        public override Task<WebhookPayloadResponse> ReceivedWebhookPayload(WebhookPayloadRequest request, ServerCallContext context)
        {
            var json = request.Payload;
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogError($"JSON payload was null, unable to deserialize webhook payload");
                return null;
            }
            _logger.LogInformation($"Host: {context.Host}");

            // TODO: Decide whether to deserialize webhook payload json here or in relay service
            _webhookRelayService.Enqueue(request.PayloadType, request.Payload);

            var response = new WebhookPayloadResponse
            {
                Status = WebhookPayloadStatus.Ok,
            };
            return Task.FromResult(response);
        }
    }
}