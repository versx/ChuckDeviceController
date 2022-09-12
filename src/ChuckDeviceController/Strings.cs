namespace ChuckDeviceController
{
    using System.Reflection;

    public static class Strings
    {
        private static readonly AssemblyName StrongAssemblyName = Assembly.GetExecutingAssembly().GetName();

        // File assembly details
        public static readonly string AssemblyName = StrongAssemblyName?.Name ?? "ChuckDeviceController";
        public static readonly string AssemblyVersion = StrongAssemblyName?.Version?.ToString() ?? "v1.0.0";

        // Default queue properties
        public const int MaximumQueueBatchSize = 25;
        public const uint MaximumQueueSizeWarning = 500;
        public const ushort MaximumQueueCapacity = 8192;
    }

    public class ProtoDataStatistics
    {
        #region Singleton

        private static ProtoDataStatistics? _instance;
        public static ProtoDataStatistics Instance => _instance ??= new();

        #endregion

        public uint TotalPayloadsReceived { get; internal set; }

        public uint TotalProtosSent { get; internal set; }

        public uint TotalEntitiesReceived { get; internal set; }

        public uint TotalEntitiesUpserted { get; internal set; }
    }
}