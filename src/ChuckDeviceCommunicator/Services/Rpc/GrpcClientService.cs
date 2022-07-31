namespace ChuckDeviceCommunicator.Services.Rpc
{
    using Grpc.Net.Client;

    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly string _grpcConfiguratorServerEndpoint;

        public GrpcClientService(IConfiguration configuration)
        {
            var configuratorEndpoint = configuration.GetValue<string>("ConfiguratorServerEndpoint");
            if (string.IsNullOrEmpty(configuratorEndpoint))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(configuratorEndpoint));
            }
            _grpcConfiguratorServerEndpoint = configuratorEndpoint;
        }

        /// <summary>
        ///     Sends a gRPC request to retrieve the latest available webhook endpoints
        ///     from the configurator.
        /// </summary>
        /// <returns>
        ///     Returns the webhook endpoint response containing the available
        ///     webhook endpoints.
        /// </returns>
        public async Task<WebhookEndpointResponse> GetWebhookEndpointsAsync()
        {
            // Create gRPC channel for receiving gRPC server address
            using var channel = GrpcChannel.ForAddress(_grpcConfiguratorServerEndpoint);

            // Create new gRPC client for gRPC channel for address
            var client = new WebhookEndpoint.WebhookEndpointClient(channel);

            // Create gRPC payload request
            var request = new WebhookEndpointRequest();

            // Handle the response of the request
            var response = await client.ReceivedWebhookEndpointAsync(request);
            return response;
        }
    }
}
