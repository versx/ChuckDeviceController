namespace ChuckDeviceController
{
    using System.Text.Json.Serialization;

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
        public DataEntityTime AverageTime
        {
            get
            {
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

    public class DataEntityTime : IComparable<DataEntityTime>
    {
        [JsonPropertyName("id")]
        public Guid Id { get; }

        [JsonPropertyName("count")]
        public ulong Count { get; set; }

        [JsonPropertyName("time_s")]
        public double TimeS { get; set; }

        public DataEntityTime()
        {
            Id = Guid.NewGuid();
        }

        public DataEntityTime(ulong count, double timeS)
            : this()
        {
            Count = count;
            TimeS = timeS;
        }

        public int CompareTo(DataEntityTime? other)
        {
            if (other == null)
                return -1;

            if (other.Id != Id)
                return -1;

            return 0;
        }
    }
}