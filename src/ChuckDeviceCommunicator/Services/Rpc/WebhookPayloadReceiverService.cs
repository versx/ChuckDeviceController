namespace ChuckDeviceCommunicator.Services.Rpc
{
    using Grpc.Core;

    using ChuckDeviceController.Protos;

    public class WebhookPayloadReceiverService : WebhookPayload.WebhookPayloadBase, IWebhookPayloadReceiverService
    {
        private readonly ILogger<IWebhookPayloadReceiverService> _logger;
        private readonly IWebhookRelayService _webhookRelayService;

        public WebhookPayloadReceiverService(
            ILogger<IWebhookPayloadReceiverService> logger,
            IWebhookRelayService webhookRelayService)
        {
            _logger = logger;
            _webhookRelayService = webhookRelayService;
        }

        public override Task<WebhookPayloadResponse> ReceivedWebhookPayload(WebhookPayloadRequest request, ServerCallContext context)
        {
            //_logger.LogInformation($"Received {request.PayloadType} webhook payload proto message");

            var json = request.Payload;
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogError($"JSON payload was null, unable to deserialize webhook payload");
                return null;
            }

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