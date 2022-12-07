﻿namespace ChuckDeviceController.Configuration
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
        public const int DefaultIntervalS = 3;
        public const ushort DefaultConcurrencyLevel = 10;

        public bool ClearOldForts { get; set; } = true;

        public ProcessingQueueConfig Queue { get; set; } = new();

        public ushort IntervalS { get; set; } = DefaultIntervalS;

        public ushort ParsingConcurrencyLevel { get; set; } = DefaultConcurrencyLevel;

        #region Processing Options

        public bool ProcessPlayerData { get; set; } = false;

        public bool ProcessCells { get; set; } = true;

        public bool ProcessWeather { get; set; } = true;

        public bool ProcessForts { get; set; } = true;

        public bool ProcessFortDetails { get; set; } = true;

        public bool ProcessGymInfo { get; set; } = true;

        public bool ProcessGymDefenders { get; set; } = true;

        public bool ProcessGymTrainers { get; set; } = true;

        public bool ProcessWildPokemon { get; set; } = true;

        public bool ProcessNearbyPokemon { get; set; } = true;

        public bool ProcessMapPokemon { get; set; } = true;

        public bool ProcessQuests { get; set; } = true;

        public bool ProcessEncounters { get; set; } = true;

        public bool ProcessDiskEncounters { get; set; } = true;

        #endregion
    }

    public class DataConsumerOptionsConfig : DataLogLevelOptionsConfig
    {
        public static readonly ushort DefaultQueueConcurrencyLevelMultiplier = Convert.ToUInt16(Environment.ProcessorCount * 4);
        public const int DefaultMaximumBatchSize = 1000;
        public const int DefaultIntervalS = 10;
        public const int DefaultQueueCapacity = 1024 * 1024;

        public int MaximumBatchSize { get; set; } = DefaultMaximumBatchSize;

        public ushort IntervalS { get; set; } = DefaultIntervalS;

        public ushort QueueConcurrencyLevelMultiplier { get; set; } = DefaultQueueConcurrencyLevelMultiplier;

        public int QueueCapacity { get; set; } = DefaultQueueCapacity;
    }
}