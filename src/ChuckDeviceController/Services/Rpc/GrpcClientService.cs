namespace ChuckDeviceController.Services.Rpc
{
    using Grpc.Net.Client;

    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly string _grpcConfiguratorServerEndpoint;
        private readonly string _grpcWebhookServerEndpoint;

        public GrpcClientService(IConfiguration configuration)
        {
            // TODO: Group server endpoints in config
            var configuratorEndpoint = configuration.GetValue<string>("GrpcConfiguratorServer");
            if (string.IsNullOrEmpty(configuratorEndpoint))
            {
                throw new ArgumentNullException($"gRPC configurator server endpoint is not set but is required!", nameof(configuratorEndpoint));
            }
            _grpcConfiguratorServerEndpoint = configuratorEndpoint;

            var webhookEndpoint = configuration.GetValue<string>("GrpcWebhookServer");
            if (string.IsNullOrEmpty(webhookEndpoint))
            {
                // TODO: Make optional if no webhooks are wanted
                throw new ArgumentNullException($"gRPC webhook server endpoint is not set but is required!", nameof(webhookEndpoint));
            }
            _grpcWebhookServerEndpoint = webhookEndpoint;
        }

        // Reference: https://stackoverflow.com/a/70099900
        public async Task SendRpcPayloadAsync<T>(T data, PayloadType payloadType, string? username = null, bool hasIV = false)
        {
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

        public async Task<WebhookPayloadResponse> SendWebhookPayloadAsync(WebhookPayloadType webhookType, string json)
        {
            if (string.IsNullOrEmpty(_grpcConfiguratorServerEndpoint))
            {
                // User does not want to process/receive webhooks
                return null;
            }

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
    }
}