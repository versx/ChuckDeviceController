namespace ChuckDeviceCommunicator.Services.Rpc
{
    using Grpc.Core.Interceptors;
    using Grpc.Net.Client;
    using Microsoft.Extensions.Options;


    using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Protos;

    // TODO: Register/configure via DI

    public class GrpcClientService : IGrpcClientService
    {
        public const ushort DefaultTimeoutS = 30;

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
        /// <param name="timeoutS">
        ///     Default request timeout in seconds before aborting.
        /// </param>
        /// <returns>
        ///     Returns the webhook endpoint response containing the available
        ///     webhook endpoints.
        /// </returns>
        public async Task<WebhookEndpointResponse?> GetWebhookEndpointsAsync(uint timeoutS = DefaultTimeoutS)
        {
            try
            {
                var options = new GrpcChannelOptions
                {
                    //HttpClient = new HttpClient
                    //{
                    //    Timeout = TimeSpan.FromSeconds(timeoutS),
                    //},
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true,
                        ConnectTimeout = TimeSpan.FromSeconds(timeoutS),
                        ResponseDrainTimeout = TimeSpan.FromSeconds(timeoutS),
                    },
                };

                // Create gRPC channel for receiving gRPC server address
                using var channel = GrpcChannel.ForAddress(_options.Configurator, options);

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
