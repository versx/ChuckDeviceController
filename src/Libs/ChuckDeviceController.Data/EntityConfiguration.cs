namespace ChuckDeviceController.Data;

public class EntityConfiguration
{
    #region Constants

    // Gyms
    public const ushort DefaultExRaidBossId = 150;
    public const ushort DefaultExRaidBoxxFormId = 0;

    // Pokestops
    public const ushort DefaultLureTimeS = 1800; // 30 minutes

    // Pokemon
    public const ushort DefaultTimeUnseenS = 1200; // 20 minutes
    public const ushort DefaultTimeReseenS = 600; // 10 minutes
    public const bool DefaultEnablePvp = true;
    public const bool DefaultEnableMapPokemon = true;
    public const bool DefaultEnableWeatherIvClearing = false;
    public const bool DefaultSaveSpawnpointLastSeen = true;

    #endregion

    #region Singleton

    private static EntityConfiguration? _instance;
    public static EntityConfiguration Instance =>
        _instance ??= new EntityConfiguration();

    #endregion

    #region Gym Options

    public ushort ExRaidBossId { get; set; } = DefaultExRaidBossId;
    public ushort ExRaidBossFormId { get; set; } = DefaultExRaidBoxxFormId;

    #endregion

    #region Pokestop Options

    public ushort LureTimeS { get; set; } = DefaultLureTimeS;

    #endregion

    #region Pokemon Options

    public ushort TimeUnseenS { get; set; } = DefaultTimeUnseenS;
    public ushort TimeReseenS { get; set; } = DefaultTimeReseenS;
    public bool EnablePvp { get; set; } = DefaultEnablePvp;
    public bool EnableMapPokemon { get; set; } = DefaultEnableMapPokemon;
    public bool EnableWeatherIvClearing { get; set; } = DefaultEnableWeatherIvClearing;
    public bool SaveSpawnpointLastSeen { get; set; } = DefaultSaveSpawnpointLastSeen;

    #endregion

    #region Public Methods

    public void LoadGymOptions(GymOptions options)
    {
        ExRaidBossId = options.ExRaidBossId;
        ExRaidBossFormId = options.ExRaidBossFormId;
    }

    public void LoadPokestopOptions(PokestopOptions options)
    {
        LureTimeS = options.LureTimeS;
    }

    public void LoadPokemonOptions(PokemonOptions options)
    {
        TimeUnseenS = options.TimeUnseenS;
        TimeReseenS = options.TimeReseenS;
        EnablePvp = options.EnablePvp;
        EnableMapPokemon = options.EnableMapPokemon;
        EnableWeatherIvClearing = options.EnableWeatherIvClearing;
        SaveSpawnpointLastSeen = options.SaveSpawnpointLastSeen;
    }

    #endregion
}

public class GymOptions
{
    public ushort ExRaidBossId { get; set; }

    public ushort ExRaidBossFormId { get; set; }
}

public class PokestopOptions
{
    public ushort LureTimeS { get; set; }
}

public class PokemonOptions
{
    public ushort TimeUnseenS { get; set; }

    public ushort TimeReseenS { get; set; }

    public bool EnablePvp { get; set; }

    public bool EnableMapPokemon { get; set; }

    public bool EnableWeatherIvClearing { get; set; }

    public bool SaveSpawnpointLastSeen { get; set; }
}