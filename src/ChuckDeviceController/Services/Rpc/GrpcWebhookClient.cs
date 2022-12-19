namespace ChuckDeviceController.Services.Rpc;

using ChuckDeviceController.Protos;

public class GrpcWebhookClient : IGrpcClient<WebhookPayload.WebhookPayloadClient, WebhookPayloadRequest, WebhookPayloadResponse>
{
    private readonly ILogger<GrpcWebhookClient> _logger;
    private readonly WebhookPayload.WebhookPayloadClient _client;
    private readonly bool _webhooksEnabled;

    public GrpcWebhookClient(
        ILogger<GrpcWebhookClient> logger,
        WebhookPayload.WebhookPayloadClient client,
        IConfiguration configuration)
    {
        _logger = logger;
        _client = client;
        _webhooksEnabled = configuration.GetValue<bool>("Webhooks:Enabled");
    }

    public async Task<WebhookPayloadResponse?> SendAsync(WebhookPayloadRequest payload)
    {
        if (!_webhooksEnabled)
        {
            return null;
        }

        try
        {
            var response = await _client.HandleWebhookPayloadAsync(payload);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.InnerException?.Message ?? ex.Message}");
        }
        return null;
    }
}