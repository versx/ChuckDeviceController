namespace ChuckDeviceCommunicator.Services.Rpc
{
    using Grpc.Core.Interceptors;
    using Grpc.Net.Client;

    using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly string _grpcConfiguratorServerEndpoint;
        private readonly AuthHeadersInterceptor _authHeadersInterceptor;

        public GrpcClientService(
            IConfiguration configuration,
            AuthHeadersInterceptor authHeadersInterceptor)
        {
            var configuratorEndpoint = configuration.GetValue<string>("GrpcConfiguratorServer");
            if (string.IsNullOrEmpty(configuratorEndpoint))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(configuratorEndpoint));
            }
            _grpcConfiguratorServerEndpoint = configuratorEndpoint;
            _authHeadersInterceptor = authHeadersInterceptor;
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

            var invoker = channel.Intercept(_authHeadersInterceptor);

            // Create new gRPC client for gRPC channel for address
            //var client = new WebhookEndpoint.WebhookEndpointClient(channel);
            var client = new WebhookEndpoint.WebhookEndpointClient(invoker);

            // Create gRPC payload request
            var request = new WebhookEndpointRequest();

            // Handle the response of the request
            var response = await client.ReceivedWebhookEndpointAsync(request);
            return response;
        }
    }
}
