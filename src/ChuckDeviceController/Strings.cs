namespace ChuckDeviceController
{
    using System.Reflection;

    public static class Strings
    {
        private static readonly AssemblyName StrongAssemblyName = Assembly.GetExecutingAssembly().GetName();

        // File assembly details
        public static readonly string AssemblyName = StrongAssemblyName?.Name ?? "ChuckDeviceController";
        public static readonly string AssemblyVersion = StrongAssemblyName?.Version?.ToString() ?? "v1.0.0";
    }

    public class ProtoDataStatistics
    {
        private readonly List<DataEntityTime> _entityTimes = new();

        #region Singleton

        private static ProtoDataStatistics? _instance;
        public static ProtoDataStatistics Instance => _instance ??= new();

        #endregion

        public ulong TotalRequestsProcessed { get; internal set; }

        public uint TotalProtoPayloadsReceived { get; internal set; }

        public uint TotalProtosProcessed { get; internal set; }

        public uint TotalEntitiesProcessed { get; internal set; }

        public uint TotalEntitiesUpserted { get; internal set; }

        public IReadOnlyList<DataEntityTime> Times => _entityTimes;

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
        public Guid Id { get; }

        public ulong Count { get; set; }

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