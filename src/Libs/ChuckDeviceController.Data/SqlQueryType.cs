namespace ChuckDeviceController.Data;

/// <summary>
/// 
/// </summary>
public enum SqlQueryType
{
    /// <summary>
    /// S2 cell insert and update
    /// </summary>
    CellUpdateOnMerge,
    /// <summary>
    /// Weather cell insert and update
    /// </summary>
    WeatherUpdateOnMerge,
    /// <summary>
    /// Spawnpoint insert and update
    /// </summary>
    SpawnpointUpdateOnMerge,
    /// <summary>
    /// Pokestop insert and update
    /// </summary>
    PokestopUpdateOnMerge,
    /// <summary>
    /// Pokestop update, ignore lure and quest values
    /// </summary>
    PokestopIgnoreOnMerge,
    /// <summary>
    /// Gym insert or update
    /// </summary>
    GymUpdateOnMerge,
    /// <summary>
    /// Pokemon insert
    /// </summary>
    PokemonUpdateOnMerge,
    /// <summary>
    /// Pokemon update, ignore pvp and IV values
    /// </summary>
    PokemonIgnoreOnMerge,
    /// <summary>
    /// Pokestop name and url update, ignore everything else
    /// </summary>
    PokestopDetailsUpdateOnMerge,
    /// <summary>
    /// Incident insert
    /// </summary>
    IncidentUpdateOnMerge,
    /// <summary>
    /// Gym name and url update, ignore everything else
    /// </summary>
    GymDetailsUpdateOnMerge,
    /// <summary>
    /// Gym trainer insert
    /// </summary>
    GymTrainerUpdateOnMerge,
    /// <summary>
    /// Gym defender insert
    /// </summary>
    GymDefenderUpdateOnMerge,
    /// <summary>
    /// Account insert and update
    /// </summary>
    AccountUpdateOnMerge,
}