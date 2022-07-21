namespace ChuckDeviceController.Services
{
    using Grpc.Net.Client;

    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;

    public class GrpcClientService
    {
        // Reference: https://stackoverflow.com/a/70099900
        public static async Task SendRpcPayloadAsync<T>(T data, PayloadType payloadType, string? username = null, bool hasIV = false)
        {
            // TODO: Configurable gRPC host/port

            // Create gRPC channel for receiving gRPC server address
            using var channel = GrpcChannel.ForAddress("http://localhost:5002");

            // Create new gRPC client for gRPC channel for address
            var client = new Payload.PayloadClient(channel);

            // Serialize entity and send to server to deserialize
            var json = data.ToJson();

            // Create gRPC payload request
            var request = new PayloadRequest
            {
                Payload = json,
                PayloadType = payloadType,
                Username = username ?? "-", // TODO: Handle null username
                HasIV = hasIV,
            };

            // Handle the response of the request
            var reply = await client.ReceivedPayloadAsync(request);
            //Console.WriteLine($"Response: {reply?.Status}");
        }
    }
}