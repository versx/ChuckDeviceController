namespace ChuckDeviceController.Services.Rpc
{
    using Grpc.Core;
    using Grpc.Core.Interceptors;
    using Grpc.Net.Client;

    using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly string _grpcConfiguratorServerEndpoint;
        private readonly string? _grpcWebhookServerEndpoint;
        private readonly ILogger<IGrpcClientService> _logger;
        private readonly AuthHeadersInterceptor _authHeadersInterceptor;

        public GrpcClientService(
            ILogger<IGrpcClientService> logger,
            IConfiguration configuration,
            AuthHeadersInterceptor authHeadersInterceptor)
        {
            _logger = logger;
            _authHeadersInterceptor = authHeadersInterceptor;

            // TODO: Group server endpoints in config
            var configuratorEndpoint = configuration.GetValue<string>("GrpcConfiguratorServer");
            if (string.IsNullOrEmpty(configuratorEndpoint))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(configuratorEndpoint));
            }
            _grpcConfiguratorServerEndpoint = configuratorEndpoint;

            var webhookEndpoint = configuration.GetValue<string>("GrpcWebhookServer");
            if (!string.IsNullOrEmpty(webhookEndpoint))
            {
                // Make optional if no webhooks are wanted
                //throw new ArgumentNullException($"gRPC webhook server endpoint is not set but is required!", nameof(webhookEndpoint));
                _grpcWebhookServerEndpoint = webhookEndpoint;
            }
        }

        // Reference: https://stackoverflow.com/a/70099900
        public async Task SendRpcPayloadAsync<T>(T data, PayloadType payloadType, string? username = null, bool hasIV = false)
        {
            try
            {
                var (channel, invoker) = CreateClient(_grpcConfiguratorServerEndpoint);
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
            catch (Exception ex)
            {
                _logger.LogError($"Unable to send proto payload to '{_grpcConfiguratorServerEndpoint}: {ex.Message}'");
            }
        }

        public async Task<TrainerInfoResponse?> GetTrainerLevelingStatusAsync(string username)
        {
            try
            {
                var (channel, invoker) = CreateClient(_grpcConfiguratorServerEndpoint);
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
                    var response = await client.ReceivedTrainerInfoAsync(request);
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to get trainer leveling status from '{_grpcConfiguratorServerEndpoint}': {ex.Message}");
            }
            return null;
        }

        public async Task<WebhookPayloadResponse?> SendWebhookPayloadAsync(WebhookPayloadType webhookType, string json)
        {
            if (string.IsNullOrEmpty(_grpcWebhookServerEndpoint))
            {
                // User does not want to process/receive webhooks
                return null;
            }

            try
            {
                var (channel, invoker) = CreateClient(_grpcWebhookServerEndpoint);
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

        private (GrpcChannel, CallInvoker) CreateClient(string url)
        {
            // Create gRPC channel for receiving gRPC server address
            var channel = GrpcChannel.ForAddress(url);
            // Create gRPC channel interceptor to invoke gRPC client
            var invoker = channel.Intercept(_authHeadersInterceptor);
            return (channel, invoker);
        }
    }
}