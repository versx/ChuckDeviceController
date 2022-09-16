namespace ChuckDeviceController.Configuration
{
    public class ProcessorOptionsConfig
    {
        public bool ClearOldForts { get; set; } = true;

        public bool ProcessMapPokemon { get; set; } = true;

        public DataLogLevel DataProcessorLogLevel { get; set; } = DataLogLevel.Summary;

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

    [Flags]
    public enum DataLogLevel
    {
        None                  = 0,
        Summary               = 1 << 0,
        WildPokemon           = 1 << 1,
        NearbyPokemon         = 1 << 2,
        MapPokemon            = 1 << 3,
        PokemonEncounters     = 1 << 4,
        PokemonDiskEncounters = 1 << 5,
        Forts                 = 1 << 6,
        FortDetails           = 1 << 7,
        Quests                = 1 << 8,
        GymInfo               = 1 << 9,
        GymDefenders          = 1 << 10,
        GymTrainers           = 1 << 11,
        Spawnpoints           = 1 << 12,
        S2Cells               = 1 << 13,
        Weather               = 1 << 14,
        PlayerData            = 1 << 15,
        All = Summary
            | WildPokemon
            | NearbyPokemon
            | MapPokemon
            | PokemonEncounters
            | PokemonDiskEncounters
            | Forts
            | FortDetails
            | Quests
            | GymInfo
            | GymDefenders
            | GymTrainers
            | Spawnpoints
            | S2Cells
            | Weather
            | PlayerData,
    }
}