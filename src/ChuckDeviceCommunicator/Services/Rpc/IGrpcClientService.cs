namespace ChuckDeviceCommunicator.Services.Rpc
{
    using ChuckDeviceController.Protos;

    /// <summary>
    /// gRPC client service wrapper class.
    /// </summary>
    public interface IGrpcClientService
    {
        /// <summary>
        ///     Sends a gRPC request to retrieve the latest available webhook endpoints
        ///     from the configurator.
        /// </summary>
        /// <returns>
        ///     Returns the webhook endpoint response containing the available
        ///     webhook endpoints.
        /// </returns>
        Task<WebhookEndpointResponse?> GetWebhookEndpointsAsync();
    }
}