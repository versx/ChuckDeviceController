namespace ChuckDeviceConfigurator.Services.Rpc;

using Grpc.Core;

using ChuckDeviceConfigurator.Services.Webhooks;
using ChuckDeviceController.Authorization.Jwt.Attributes;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Protos;

[JwtAuthorize(Strings.Identifier)]
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

    public override async Task<WebhookEndpointResponse> HandleWebhookEndpoint(WebhookEndpointRequest request, ServerCallContext context)
    {
        _logger.LogDebug($"Received fetch webhook endpoints request from: {context.Host}");

        var webhooks = await _webhookService.GetAllAsync(includeGeofenceMultiPolygons: true);
        if (webhooks == null)
        {
            return await Task.FromResult(new WebhookEndpointResponse
            {
                Status = WebhookEndpointStatus.Error,
            });
        }

        var json = webhooks.ToJson();
        if (string.IsNullOrEmpty(json))
        {
            return await Task.FromResult(new WebhookEndpointResponse
            {
                Status = WebhookEndpointStatus.Error,
            });
        }

        var response = new WebhookEndpointResponse
        {
            Status = WebhookEndpointStatus.Ok,
            Payload = json,
        };
        return await Task.FromResult(response);
    }

    #endregion
}