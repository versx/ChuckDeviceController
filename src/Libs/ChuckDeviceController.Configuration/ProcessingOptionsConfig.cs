namespace ChuckDeviceController.Configuration
{
    public class ProcessingOptionsConfig
    {
        #region Properties

        public bool ClearOldForts { get; set; } = true;

        public bool ProcessMapPokemon { get; set; } = true;

        public DataLogLevel DataProcessorLogLevel { get; set; } = DataLogLevel.Summary;

        public QueueConfig Queue { get; set; } = new();

        #endregion

        #region Public Methods

        public bool IsEnabled(DataLogLevel logLevel)
        {
            return (DataProcessorLogLevel & logLevel) == logLevel;
        }

        public void EnableLogLevel(DataLogLevel logLevel)
        {
            DataProcessorLogLevel |= logLevel;
        }

        public void DisableLogLevel(DataLogLevel logLevel)
        {
            DataProcessorLogLevel &= (~logLevel);
        }

        #endregion
    }
}