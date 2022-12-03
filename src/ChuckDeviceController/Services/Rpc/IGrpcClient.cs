namespace ChuckDeviceController.Services.Rpc
{
    public interface IGrpcClient<TClient, TRequest, TResponse>
    {
        Task<TResponse?> SendAsync(TRequest payload);
    }
}