namespace ChuckDeviceController.Configuration
{
    [Flags]
    public enum DataLogLevel
    {
        None = 0,
        Summary = 1 << 0,
        WildPokemon = 1 << 1,
        NearbyPokemon = 1 << 2,
        MapPokemon = 1 << 3,
        PokemonEncounters = 1 << 4,
        PokemonDiskEncounters = 1 << 5,
        Forts = 1 << 6,
        FortDetails = 1 << 7,
        Quests = 1 << 8,
        GymInfo = 1 << 9,
        GymDefenders = 1 << 10,
        GymTrainers = 1 << 11,
        Incidents = 1 << 12,
        Spawnpoints = 1 << 13,
        S2Cells = 1 << 14,
        Weather = 1 << 15,
        PlayerData = 1 << 16,
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
            | Incidents
            | Spawnpoints
            | S2Cells
            | Weather
            | PlayerData,
    }
}