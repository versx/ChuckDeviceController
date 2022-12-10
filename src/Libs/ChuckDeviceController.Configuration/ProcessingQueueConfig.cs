namespace ChuckDeviceController.Configuration
{
    public class ProcessingQueueConfig
    {
        public const uint DefaultMaximumBatchSize = 100;
        public const uint DefaultMaximumSizeWarning = 1024;
        public const ushort DefaultMaximumCapacity = 10240;

        public uint MaximumBatchSize { get; set; } = DefaultMaximumBatchSize;

        public uint MaximumSizeWarning { get; set; } = DefaultMaximumSizeWarning;

        public uint MaximumCapacity { get; set; } = DefaultMaximumCapacity;

        public ProcessingQueueConfig(
            uint maximumBatchSize = DefaultMaximumBatchSize,
            uint maximumSizeWarning = DefaultMaximumSizeWarning,
            uint maximumCapacity = DefaultMaximumCapacity)
        {
            MaximumBatchSize = maximumBatchSize;
            MaximumSizeWarning = maximumSizeWarning;
            MaximumCapacity = maximumCapacity;
        }
    }
}