namespace ChuckDeviceController.Configuration
{
    public class ProcessingOptionsConfig
    {
        public ProtoProcessorOptionsConfig Protos { get; set; } = new();

        public DataProcessorOptionsConfig Data { get; set; } = new();

        public DataConsumerOptionsConfig Consumer { get; set; } = new();
    }

    public class DataLogLevelOptionsConfig
    {
        public DataLogLevel LogLevel { get; set; } = DataLogLevel.Summary;

        public bool IsEnabled(DataLogLevel logLevel)
        {
            return (LogLevel & logLevel) == logLevel;
        }

        public void EnableLogLevel(DataLogLevel logLevel)
        {
            LogLevel |= logLevel;
        }

        public void DisableLogLevel(DataLogLevel logLevel)
        {
            LogLevel &= (~logLevel);
        }
    }

    public class ProtoProcessorOptionsConfig : DataLogLevelOptionsConfig
    {
        public bool ProcessMapPokemon { get; set; } = true;

        public ProcessingQueueConfig Queue { get; set; } = new();
    }

    public class DataProcessorOptionsConfig : DataLogLevelOptionsConfig
    {
        public bool ClearOldForts { get; set; } = true;

        public ProcessingQueueConfig Queue { get; set; } = new();

        public ushort IntervalS { get; set; } = 5;
    }

    public class DataConsumerOptionsConfig : DataLogLevelOptionsConfig
    {
        public int MaximumBatchSize { get; set; } = 1000;

        public ushort IntervalS { get; set; } = 3;
    }
}