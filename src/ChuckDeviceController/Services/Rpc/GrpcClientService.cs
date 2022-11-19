namespace ChuckDeviceController.Services.Rpc
{
    using Grpc.Core;
    using Grpc.Core.Interceptors;
    using Grpc.Net.Client;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly ILogger<IGrpcClientService> _logger;
        private readonly AuthHeadersInterceptor _authHeadersInterceptor;
        private readonly GrpcEndpointsConfig _options;

        public GrpcClientService(
            ILogger<IGrpcClientService> logger,
            IOptions<GrpcEndpointsConfig> options,
            AuthHeadersInterceptor authHeadersInterceptor)
        {
            _logger = logger;
            _options = options.Value;
            _authHeadersInterceptor = authHeadersInterceptor;

            if (string.IsNullOrEmpty(_options.Configurator))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(_options.Configurator));
            }

            if (!string.IsNullOrEmpty(_options.Communicator))
            {
                // Make optional if no webhooks are wanted
                //throw new ArgumentNullException($"gRPC webhook server endpoint is not set but is required!", nameof(_options.Communicator));
            }
        }

        // Reference: https://stackoverflow.com/a/70099900
        public async Task SendRpcPayloadAsync<T>(T data, PayloadType payloadType, string? username = null, bool hasIV = false)
        {
            try
            {
                var (channel, invoker) = CreateClient(_options.Configurator);
                using (channel)
                {
                    // Create new gRPC client using gRPC channel for address
                    //var client = new Payload.PayloadClient(channel);
                    var client = new Payload.PayloadClient(invoker);

                    // Serialize entity and send to server to deserialize
                    var json = data.ToJson();

                    // Create gRPC payload request
                    var request = new PayloadRequest
                    {
                        Payload = json,
                        PayloadType = payloadType,
                        Username = username,
                        HasIV = hasIV,
                    };

                    // Handle the response of the request
                    var reply = await client.ReceivedPayloadAsync(request);
                    //Console.WriteLine($"Response: {reply?.Status}");
                }
            }
            catch //(Exception ex)
            {
                //_logger.LogWarning($"Unable to send proto payload to '{_options.Configurator}: {ex.Message}'");
                //_logger.LogWarning($"Unable to send proto payload to '{_options.Configurator}'");
            }
        }

        public async Task<TrainerInfoResponse?> GetTrainerLevelingStatusAsync(string username)
        {
            try
            {
                var (channel, invoker) = CreateClient(_options.Configurator);
                using (channel)
                {
                   // Create new gRPC client for gRPC channel for address
                   //var client = new Leveling.LevelingClient(channel);
                   var client = new Leveling.LevelingClient(invoker);

                    // Create gRPC payload request
                    var request = new TrainerInfoRequest
                    {
                        Username = username,
                    };

                    // Handle the response of the request
                    // TODO: Fix issue with GetTrainerLevelingStatusAsync
                    var response = await client.ReceivedTrainerInfoAsync(request);
                    return response;
                }
            }
            catch //(Exception ex)
            {
                //_logger.LogWarning($"Unable to get trainer leveling status from '{_options.Configurator}': {ex.Message}");
                //_logger.LogWarning($"Unable to get trainer leveling status from '{_options.Configurator}'");
            }
            return null;
        }

        public async Task<WebhookPayloadResponse?> SendWebhookPayloadAsync(WebhookPayloadType webhookType, string json)
        {
            if (string.IsNullOrEmpty(_options.Communicator))
            {
                // User does not want to process/receive webhooks
                return null;
            }

            try
            {
                var (channel, invoker) = CreateClient(_options.Communicator);
                using (channel)
                {
                    // Create new gRPC client using gRPC channel for address
                    //var client = new WebhookPayload.WebhookPayloadClient(channel);
                    var client = new WebhookPayload.WebhookPayloadClient(invoker);

                    // Create gRPC payload request
                    var request = new WebhookPayloadRequest
                    {
                        PayloadType = webhookType,
                        Payload = json,
                    };

                    // Handle the response of the request
                    var response = await client.ReceivedWebhookPayloadAsync(request);
                    return response;
                }
            }
            catch //(Exception ex)
            {
                //_logger.LogWarning($"Unable to send webhook to webhook relay service at '{_grpcWebhookServerEndpoint}: {ex.Message}'");
            }
            return null;
        }

        private (GrpcChannel, CallInvoker) CreateClient(string url, uint timeoutS = 3)
        {
            // Create gRPC channel options
            var options = new GrpcChannelOptions
            {
                HttpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(timeoutS),
                },
            };
            // Create gRPC channel for receiving gRPC server address
            var channel = GrpcChannel.ForAddress(url, options);
            // Create gRPC channel interceptor to invoke gRPC client
            var invoker = channel.Intercept(_authHeadersInterceptor);
            return (channel, invoker);
        }
    }
}