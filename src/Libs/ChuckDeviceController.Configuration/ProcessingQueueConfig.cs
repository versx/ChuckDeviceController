namespace ChuckDeviceController.Configuration
{
    public class ProcessingQueueConfig
    {
        public uint MaximumBatchSize { get; set; } = 100;

        public uint MaximumSizeWarning { get; set; } = 500;

        public ushort MaximumCapacity { get; set; } = 8192;
    }
}