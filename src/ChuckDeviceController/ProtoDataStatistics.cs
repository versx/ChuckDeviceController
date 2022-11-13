namespace ChuckDeviceController
{
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common;

    public class ProtoDataStatistics
    {
        private readonly List<DataEntityTime> _entityTimes = new();

        #region Singleton

        private static ProtoDataStatistics? _instance;
        public static ProtoDataStatistics Instance => _instance ??= new();

        #endregion

        [JsonPropertyName("total_requests")]
        public ulong TotalRequestsProcessed { get; internal set; }

        [JsonPropertyName("protos_received")]
        public uint TotalProtoPayloadsReceived { get; internal set; }

        [JsonPropertyName("protos_processed")]
        public uint TotalProtosProcessed { get; internal set; }

        [JsonPropertyName("entities_processed")]
        public uint TotalEntitiesProcessed { get; internal set; }

        [JsonPropertyName("entities_upserted")]
        public uint TotalEntitiesUpserted { get; internal set; }

        [JsonPropertyName("data_times")]
        public IReadOnlyList<DataEntityTime> Times => _entityTimes;

        [JsonPropertyName("avg_time")]
        public DataEntityTime? AverageTime
        {
            get
            {
                if (!(Times?.Any() ?? false))
                {
                    return default;
                }
                var count = _entityTimes.Average(time => (decimal)time.Count);
                var time = _entityTimes.Average(time => (decimal)time.TimeS);
                return new(Convert.ToUInt64(count), Convert.ToUInt64(time));
            }
        }

        internal void AddTimeEntry(DataEntityTime entity)
        {
            if (!_entityTimes.Contains(entity))
            {
                _entityTimes.Add(entity);
            }
        }
    }
}