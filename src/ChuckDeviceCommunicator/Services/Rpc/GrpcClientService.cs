namespace ChuckDeviceCommunicator.Services.Rpc
{
    using Grpc.Core.Interceptors;
    using Grpc.Net.Client;
    using Microsoft.Extensions.Options;


    using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly ILogger<IGrpcClientService> _logger;
        private readonly GrpcEndpointsConfig _options;
        private readonly AuthHeadersInterceptor _authHeadersInterceptor;

        public GrpcClientService(
            ILogger<IGrpcClientService> logger,
            IOptions<GrpcEndpointsConfig> options,
            AuthHeadersInterceptor authHeadersInterceptor)
        {
            _options = options.Value;

            if (string.IsNullOrEmpty(_options.Configurator))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(_options.Configurator));
            }

            _logger = logger;
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
        public async Task<WebhookEndpointResponse?> GetWebhookEndpointsAsync()
        {
            try
            {
                // Create gRPC channel for receiving gRPC server address
                using var channel = GrpcChannel.ForAddress(_options.Configurator);

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
            catch (Exception ex)
            {
                _logger.LogError($"Unable to send webhook to webhook relay service at '{_options.Configurator}: {ex.Message}'");
            }
            return null;
        }
    }
}
