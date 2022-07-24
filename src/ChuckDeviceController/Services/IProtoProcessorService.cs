namespace ChuckDeviceController.Services
{
    public interface IProtoProcessorService
    {
        Task EnqueueAsync(ProtoPayloadQueueItem payload);
    }
}