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
            // TODO: Configurable host/port
            using var channel = GrpcChannel.ForAddress("http://localhost:5002");
            var client = new Payload.PayloadClient(channel);

            // Serialize entity and send to server to deserialize
            var json = data.ToJson();
            var request = new PayloadRequest
            {
                Payload = json,
                PayloadType = payloadType,
                Username = username ?? "-", // TODO: Handle null username
                HasIV = hasIV,
            };
            var reply = await client.ReceivedPayloadAsync(request);
            //Console.WriteLine($"Response: {reply?.Status}");
        }
    }
}