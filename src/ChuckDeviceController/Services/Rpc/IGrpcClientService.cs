namespace ChuckDeviceController.Services.Rpc
{
    using ChuckDeviceController.Protos;

    public interface IGrpcClientService
    {
        Task SendRpcPayloadAsync<T>(T data, PayloadType payloadType, string? username = null, bool hasIV = false);

        Task<TrainerInfoResponse> GetTrainerLevelingStatusAsync(string username);

        Task<WebhookPayloadResponse> SendWebhookAsync(WebhookPayloadType webhookType, string json);
    }
}