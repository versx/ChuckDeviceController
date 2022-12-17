namespace ChuckDeviceController.Data
{
    public class EntityConfiguration
    {
        #region Constants

        public const ushort DefaultExRaidBossId = 150;
        public const ushort DefaultExRaidBoxxFormId = 0;

        public const ushort DefaultLureTimeS = 1800; // 30 minutes

        public const bool DefaultEnablePvp = true;
        public const bool DefaultEnableMapPokemon = true;
        public const bool DefaultEnableWeatherIvClearing = false;
        public const bool DefaultSaveSpawnpointLastSeen = true;

        #endregion

        #region Gym Options

        public static ushort ExRaidBossId { get; set; } = DefaultExRaidBossId;
        public static ushort ExRaidBossFormId { get; set; } = DefaultExRaidBoxxFormId;

        #endregion

        #region Pokestop Options

        public static ushort LureTimeS { get; set; } = DefaultLureTimeS;

        #endregion

        #region Pokemon Options

        public static bool EnablePvp { get; set; } = DefaultEnablePvp;
        public static bool EnableMapPokemon { get; set; } = DefaultEnableMapPokemon;
        public static bool EnableWeatherIvClearing { get; set; } = DefaultEnableWeatherIvClearing;
        public static bool SaveSpawnpointLastSeen { get; set; } = DefaultSaveSpawnpointLastSeen;

        #endregion

        #region Public Methods

        public static void LoadGymOptions(GymOptions options)
        {
            ExRaidBossId = options.ExRaidBossId;
            ExRaidBossFormId = options.ExRaidBossFormId;
        }

        public static void LoadPokestopOptions(PokestopOptions options)
        {
            LureTimeS = options.LureTimeS;
        }

        public static void LoadPokemonOptions(PokemonOptions options)
        {
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
        public bool EnablePvp { get; set; }

        public bool EnableMapPokemon { get; set; }

        public bool EnableWeatherIvClearing { get; set; }

        public bool SaveSpawnpointLastSeen { get; set; }
    }
}