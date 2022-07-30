namespace ChuckDeviceConfigurator.Services.Rpc
{
    using Grpc.Core;

    using ChuckDeviceConfigurator.Services.Webhooks;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Protos;

    public class WebhookEndpointServerService : WebhookEndpoint.WebhookEndpointBase
    {
        #region Variables

        private readonly ILogger<WebhookEndpointServerService> _logger;
        private readonly IWebhookControllerService _webhookService;

        #endregion

        #region Constructor

        public WebhookEndpointServerService(
            ILogger<WebhookEndpointServerService> logger,
            IWebhookControllerService webhookService)
        {
            _logger = logger;
            _webhookService = webhookService;
        }

        #endregion

        #region Event Handlers

        public override Task<WebhookEndpointResponse> ReceivedWebhookEndpoint(WebhookEndpointRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Received fetch webhook endpoints request");

            var webhooks = _webhookService.GetAll();
            var json = webhooks.ToJson();

            // TODO: Provide error status if failed to get webhooks

            var response = new WebhookEndpointResponse
            {
                Status = WebhookEndpointStatus.Ok,
                Payload = json,
            };
            return Task.FromResult(response);
        }

        #endregion
    }
}