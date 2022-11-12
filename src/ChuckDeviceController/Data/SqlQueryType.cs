namespace ChuckDeviceController.Data
{
    public enum SqlQueryType
    {
        /// <summary>
        /// Account insert and update
        /// </summary>
        AccountOnMergeUpdate,
        /// <summary>
        /// Pokemon insert
        /// </summary>
        PokemonOnMergeUpdate,
        /// <summary>
        /// Pokemon update, ignore pvp and IV values
        /// </summary>
        PokemonIgnoreOnMerge,
        /// <summary>
        /// Pokestop insert and update
        /// </summary>
        PokestopOnMergeUpdate,
        /// <summary>
        /// Pokestop update, ignore lure and quest values
        /// </summary>
        PokestopIgnoreOnMerge,
        /// <summary>
        /// Pokestop name and url update, ignore everything else
        /// </summary>
        PokestopDetailsOnMergeUpdate,
        /// <summary>
        /// Incident insert
        /// </summary>
        IncidentOnMergeUpdate,
        /// <summary>
        /// Gym insert or update
        /// </summary>
        GymOnMergeUpdate,
        /// <summary>
        /// Gym name and url update, ignore everything else
        /// </summary>
        GymDetailsOnMergeUpdate,
        /// <summary>
        /// Gym defender insert
        /// </summary>
        GymDefenderOnMergeUpdate,
        /// <summary>
        /// Gym trainer insert
        /// </summary>
        GymTrainerOnMergeUpdate,
        /// <summary>
        /// Spawnpoint insert and update
        /// </summary>
        SpawnpointOnMergeUpdate,
        /// <summary>
        /// S2 cell insert and update
        /// </summary>
        CellOnMergeUpdate,
        /// <summary>
        /// Weather cell insert and update
        /// </summary>
        WeatherOnMergeUpdate,
    }
}