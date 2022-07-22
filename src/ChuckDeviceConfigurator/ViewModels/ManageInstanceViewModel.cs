namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    using ChuckDeviceController.Data;

    public class ManageInstanceViewModel
    {
        [DisplayName("Name")]
        public string Name { get; set; }

        // NOTE: Set to nullable so default value when creating an instance isn't set to `0` aka CirclePokemon
        [DisplayName("Instance Type")]
        public InstanceType? Type { get; set; }

        [DisplayName("Minimum Level")]
        public ushort MinimumLevel { get; set; }

        [DisplayName("Maximum Level")]
        public ushort MaximumLevel { get; set; }

        [DisplayName("Geofences")]
        public List<string> Geofences { get; set; } = new();

        [DisplayName("Instance Data")]
        public ManageInstanceDataViewModel Data { get; set; } = new();
    }

    public class ManageInstanceDataViewModel
    {
        // Circle Instance
        [DisplayName("Circle Instance Route Type")]
        public CircleInstanceRouteType? CircleRouteType { get; set; } = Strings.DefaultCircleRouteType;


        // Dynamic Route Instance
        [DisplayName("Optimize Dynamic Route")]
        public bool OptimizeDynamicRoute { get; set; } = Strings.DefaultOptimizeDynamicRoute;


        // Quest Instance
        [DisplayName("Time Zone")]
        public string? TimeZone { get; set; } = Strings.DefaultTimeZone;

        [DisplayName("Enable DST")]
        public bool EnableDst { get; set; } = Strings.DefaultEnableDst;

        [DisplayName("Spin Limit")]
        public ushort? SpinLimit { get; set; } = Strings.DefaultSpinLimit;

        [DisplayName("Ignore S2 Cell Bootstrapping")]
        public bool IgnoreS2CellBootstrap { get; set; } = Strings.DefaultIgnoreS2CellBootstrap;

        [DisplayName("Use Red Warning Accounts")]
        public bool UseWarningAccounts { get; set; } = Strings.DefaultUseWarningAccounts;

        [DisplayName("Quest Mode")]
        public QuestMode? QuestMode { get; set; } = Strings.DefaultQuestMode;

        [DisplayName("Maximum Pokestop Spin Attempts")]
        public byte? MaximumSpinAttempts { get; set; } = Strings.DefaultMaximumSpinAttempts;

        [DisplayName("Account Logout Delay")]
        public ushort? LogoutDelay { get; set; } = Strings.DefaultLogoutDelay;


        // IV Instance
        [DisplayName("IV Queue Limit")]
        public ushort? IvQueueLimit { get; set; } = Strings.DefaultIvQueueLimit;

        [DisplayName("IV List")]
        public string? IvList { get; set; } = Strings.DefaultIvList;

        [DisplayName("Enable Lure Encounters")]
        public bool EnableLureEncounters { get; set; } = Strings.DefaultEnableLureEncounters;


        // Bootstrap Instance
        [DisplayName("Fast Bootstrap Mode")]
        public bool FastBootstrapMode { get; set; } = Strings.DefaultFastBootstrapMode;

        [DisplayName("Circle Size")]
        public ushort? CircleSize { get; set; } = Strings.DefaultCircleSize;

        [DisplayName("Optimize Bootstrap Route")]
        public bool OptimizeBootstrapRoute { get; set; } = Strings.DefaultOptimizeBootstrapRoute;

        [DisplayName("Bootstrap Complete Instance Name")]
        public string? BootstrapCompleteInstanceName { get; set; } = Strings.DefaultBootstrapCompleteInstanceName;


        // Spawnpoint Instance
        [DisplayName("Only Unknown Spawnpoints")]
        public bool OnlyUnknownSpawnpoints { get; set; } = Strings.DefaultOnlyUnknownSpawnpoints;

        [DisplayName("Optimize Spawnpoints Route")]
        public bool OptimizeSpawnpointsRoute { get; set; } = Strings.DefaultOptimizeBootstrapRoute;


        // Leveling Instance
        [DisplayName("Leveling Radius")]
        public uint LevelingRadius { get; set; } = Strings.DefaultLevelingRadius;

        [DisplayName("Save Leveling Data")]
        public bool StoreLevelingData { get; set; } = Strings.DefaultStoreLevelingData;


        // All
        [DisplayName("Account Group")]
        public string? AccountGroup { get; set; } = Strings.DefaultAccountGroup;

        [DisplayName("Is Unique Event")]
        public bool IsEvent { get; set; } = Strings.DefaultIsEvent;
    }
}