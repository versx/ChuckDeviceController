namespace ChuckDeviceConfigurator.Services.Rpc
{
    using Grpc.Core;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceController.Protos;

    public class WebhookEndpointServerService : WebhookEndpoint.WebhookEndpointBase
    {
        #region Variables

        private readonly ILogger<ProtoPayloadServerService> _logger;
        private readonly IJobControllerService _jobControllerService;

        #endregion

        #region Constructor

        public WebhookEndpointServerService(
            ILogger<ProtoPayloadServerService> logger,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _jobControllerService = jobControllerService;
        }

        #endregion

        #region Event Handlers

        public override Task<WebhookEndpointResponse> ReceivedWebhookEndpoint(WebhookEndpointRequest request, ServerCallContext context)
        {
            //_logger.LogInformation($"Received webhook endpoints request");

            // TODO: Get all webhooks from database
            var json = string.Empty;

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