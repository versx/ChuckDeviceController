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

        public bool ProcessPlayerData { get; set; } = false;

        public bool ProcessCells { get; set; } = true;

        public bool ProcessWeather { get; set; } = true;

        public bool ProcessForts { get; set; } = false;

        public bool ProcessFortDetails { get; set; } = true;

        public bool ProcessGymInfo { get; set; } = true;

        // TODO: ProcessGymDefenders

        // TODO: ProcessGymTrainers

        public bool ProcessPokemon { get; set; } = true;

        // TODO: ProcessWildPokemon

        // TODO: ProcessNearbyPokemon

        // TODO: ProcessMapPokemon

        public bool ProcessQuests { get; set; } = true;

        public bool ProcessEncounters { get; set; } = false;

        public bool ProcessDiskEncounters { get; set; } = true;
    }

    public class DataConsumerOptionsConfig : DataLogLevelOptionsConfig
    {
        public int MaximumBatchSize { get; set; } = 1000;

        public ushort IntervalS { get; set; } = 3;
    }
}