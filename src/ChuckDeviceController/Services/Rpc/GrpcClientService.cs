namespace ChuckDeviceController.Services.Rpc
{
    using Grpc.Net.Client;

    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly string _grpcConfiguratorServerEndpoint;
        private readonly string? _grpcWebhookServerEndpoint;
        private readonly ILogger<IGrpcClientService> _logger;

        public GrpcClientService(
            ILogger<IGrpcClientService> logger,
            IConfiguration configuration)
        {
            _logger = logger;

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
            if (string.IsNullOrEmpty(_grpcConfiguratorServerEndpoint))
            {
                throw new Exception($"ChuckDeviceConfigurator gRPC server endpoint is empty but is required!");
            }

            // Create gRPC channel for receiving gRPC server address
            using var channel = GrpcChannel.ForAddress(_grpcConfiguratorServerEndpoint);

            // Create new gRPC client for gRPC channel for address
            var client = new Payload.PayloadClient(channel);

            // Serialize entity and send to server to deserialize
            var json = data.ToJson();

            // Create gRPC payload request
            var request = new PayloadRequest
            {
                Payload = json,
                PayloadType = payloadType,
                Username = username ?? "-", // TODO: Handle null username, StringValue in proto
                HasIV = hasIV,
            };

            // Handle the response of the request
            var reply = await client.ReceivedPayloadAsync(request);
            //Console.WriteLine($"Response: {reply?.Status}");
        }

        public async Task<TrainerInfoResponse> GetTrainerLevelingStatusAsync(string username)
        {
            if (string.IsNullOrEmpty(_grpcConfiguratorServerEndpoint))
            {
                throw new Exception($"ChuckDeviceConfigurator gRPC server endpoint is empty but is required!");
            }

            // Create gRPC channel for receiving gRPC server address
            using var channel = GrpcChannel.ForAddress(_grpcConfiguratorServerEndpoint);

            // Create new gRPC client for gRPC channel for address
            var client = new Leveling.LevelingClient(channel);

            // Create gRPC payload request
            var request = new TrainerInfoRequest
            {
                Username = username,
            };

            // Handle the response of the request
            var response = await client.ReceivedTrainerInfoAsync(request);
            return response;
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
                // Create gRPC channel for receiving gRPC server address
                using var channel = GrpcChannel.ForAddress(_grpcWebhookServerEndpoint);

                // Create new gRPC client for gRPC channel for address
                var client = new WebhookPayload.WebhookPayloadClient(channel);

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
            catch //(Exception ex)
            {
                _logger.LogWarning($"Unable to send webhook to webhook relay service at '{_grpcWebhookServerEndpoint}'");
            }
            return null;
        }
    }
}