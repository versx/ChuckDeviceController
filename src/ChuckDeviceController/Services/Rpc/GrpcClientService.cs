namespace ChuckDeviceController.Services.Rpc
{
    using Grpc.Net.Client;

    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;

    public class GrpcClientService : IGrpcClientService
    {
        private readonly string _grpcServerEndpoint;

        public GrpcClientService(IConfiguration configuration)
        {
            var endpoint = configuration.GetValue<string>("GrpcServer");
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException($"gRPC server endpoint is not set but is required!");
            }
            _grpcServerEndpoint = endpoint;
        }

        // Reference: https://stackoverflow.com/a/70099900
        public async Task SendRpcPayloadAsync<T>(T data, PayloadType payloadType, string? username = null, bool hasIV = false)
        {
            // Create gRPC channel for receiving gRPC server address
            using var channel = GrpcChannel.ForAddress(_grpcServerEndpoint);

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
    }
}