namespace ChuckDeviceController.Configuration
{
    public class QueueConfig
    {
        public ProcessingQueueConfig Protos { get; set; } = new();

        public ProcessingQueueConfig Data { get; set; } = new();
    }
}