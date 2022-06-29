namespace ChuckDeviceController.Services
{
    public interface IProtoProcessorService
    {
        Task EnqueueAsync(ProtoPayloadItem payload);
    }
}