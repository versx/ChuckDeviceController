namespace ChuckDeviceController.Extensions.Http.Caching
{
    public class EntityMemoryCacheConfig
    {
        public double CompactionPercentage { get; set; } = 0.25;

        public ushort ExpirationScanFrequencyM { get; set; } = 5;

        public uint SizeLimit { get; set; } = 10240;

        public ushort EntityExpiryLimitM { get; set; } = 15;
    }
}