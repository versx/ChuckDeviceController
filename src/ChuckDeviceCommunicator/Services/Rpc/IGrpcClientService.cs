namespace ChuckDeviceCommunicator.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public interface IGrpcClientService
    {
        Task<WebhookEndpointResponse> GetWebhookEndpointsAsync();
    }
}