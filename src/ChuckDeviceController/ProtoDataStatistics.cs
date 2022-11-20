namespace ChuckDeviceController
{
    using ChuckDeviceController.Common;

    public class ProtoDataStatistics : BaseProtoDataStatistics
    {
        #region Variables

        private readonly List<DataEntityTime> _entityTimes = new();
        private readonly object _lock = new();

        #endregion

        #region Singleton

        private static ProtoDataStatistics? _instance;
        public static ProtoDataStatistics Instance => _instance ??= new();

        #endregion

        #region Properties

        public override IReadOnlyList<DataEntityTime> Times => _entityTimes; // Data consumer times

        public override DataEntityTime? AverageTime
        {
            get
            {
                lock (_lock)
                {
                    if (!(_entityTimes?.Any() ?? false))
                    {
                        return default;
                    }
                    var count = _entityTimes.Average(time => (double)time.Count);
                    var time = Math.Round(_entityTimes.Average(time => (double)time.TimeS), 5);
                    return new(Convert.ToUInt64(count), time);
                }
            }
        }

        #endregion

        #region Internal Methods

        internal void AddTimeEntry(DataEntityTime entity)
        {
            lock (_lock)
            {
                if (!_entityTimes.Contains(entity))
                {
                    _entityTimes.Add(entity);
                }
            }
        }

        internal void Reset()
        {
            TotalRequestsProcessed = 0;
            TotalProtoPayloadsReceived = 0;
            TotalProtosProcessed = 0;
            TotalEntitiesProcessed = 0;
            TotalEntitiesUpserted = 0;

            lock (_lock)
            {
                _entityTimes.Clear();
            }
        }

        #endregion
    }
}