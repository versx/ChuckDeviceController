namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;

    /* TODO: Possibly use the same `OptimizeRoute` property between Dynamic/Bootstrap/Spawnpoint
     * job controllers for ease. Can't think of any conflicts/issues unless someone edits an
     * existing instance the UI will display the previous value instead of using the default,
     * which is fine. :)
    */
    public class InstanceData : IInstanceData
    {
        #region Pokemon Circle Instance
        [
            DisplayName("Circle Route Type"),
            JsonPropertyName("circle_route_type"),
        ]
        public CircleInstanceRouteType? CircleRouteType { get; set; } = CircleInstanceRouteType.Default;

        #endregion

        #region Dynamic Route Instance

        [
            DisplayName("Optimize Dynamic Route"),
            JsonPropertyName("optimize_dynamic_route"),
        ]
        public bool OptimizeDynamicRoute { get; set; }

        #endregion

        #region Quest Instance

        [
            DisplayName("Time Zone"),
            JsonPropertyName("timezone"),
        ]
        public string? TimeZone { get; set; }

        [
            DisplayName("Enable DST"),
            JsonPropertyName("enable_dst"),
        ]
        public bool? EnableDst { get; set; }

        [
            DisplayName("Spin Limit"),
            JsonPropertyName("spin_limit"),
        ]
        public ushort? SpinLimit { get; set; }

        [
            DisplayName("Ignore S2 Cell Bootstrapping"),
            JsonPropertyName("ignore_s2_cell_bootstrap"),
        ]
        public bool? IgnoreS2CellBootstrap { get; set; }

        [
            DisplayName("Use Red Warning Accounts"),
            JsonPropertyName("use_warning_accounts"),
        ]
        public bool? UseWarningAccounts { get; set; }

        [
            DisplayName("Quest Mode"),
            JsonPropertyName("quest_mode"),
        ]
        public QuestMode? QuestMode { get; set; } = Common.Data.QuestMode.Normal;

        [
            DisplayName("Maximum Pokestop Spin Attempts"),
            JsonPropertyName("max_spin_attempts"),
        ]
        public byte MaximumSpinAttempts { get; set; }

        [
            DisplayName("Logout Delay"),
            JsonPropertyName("logout_delay"),
        ]
        public ushort LogoutDelay { get; set; }

        #endregion

        #region IV Instance

        [
            DisplayName("IV Queue Limit"),
            JsonPropertyName("iv_queue_limit"),
        ]
        public ushort? IvQueueLimit { get; set; }

        [
            DisplayName("IV List"),
            JsonPropertyName("iv_list"),
        ]
        public string? IvList { get; set; }

        [
            DisplayName("Enable Lure Encounters"),
            JsonPropertyName("enable_lure_encounters"),
        ]
        public bool? EnableLureEncounters { get; set; }

        #endregion

        #region Bootstrap Instance

        [
            DisplayName("Fast Bootstrap Mode"),
            JsonPropertyName("fast_bootstrap_mode"),
        ]
        public bool? FastBootstrapMode { get; set; }

        [
            DisplayName("Circle Size"),
            JsonPropertyName("circle_size"),
        ]
        public ushort? CircleSize { get; set; }

        [
            DisplayName("Optimize Bootstrap Route"),
            JsonPropertyName("optimize_bootstrap_route"),
        ]
        public bool OptimizeBootstrapRoute { get; set; }

        [
            DisplayName("Bootstrap Complete Instance Name"),
            JsonPropertyName("bootstrap_complete_instance_name"),
        ]
        public string? BootstrapCompleteInstanceName { get; set; }

        #endregion

        #region Spawnpoint Instance

        [
            DisplayName("Optimize Spawnpoints Route"),
            JsonPropertyName("optimize_spawnpoints_route"),
        ]
        public bool OptimizeSpawnpointsRoute { get; set; }

        [
            DisplayName("Only Unknown Spawnpoints"),
            JsonPropertyName("only_unknown_spawnpoints"),
        ]
        public bool OnlyUnknownSpawnpoints { get; set; }

        #endregion

        #region Leveling Instance

        [
            DisplayName("Leveling Radius"),
            JsonPropertyName("leveling_radius"),
        ]
        public uint LevelingRadius { get; set; }

        [
            DisplayName("Save Leveling Data"),
            JsonPropertyName("store_leveling_data"),
        ]
        public bool StoreLevelingData { get; set; }

        [
            DisplayName("Starting Coordinate"),
            JsonPropertyName("leveling_start_coordinate"),
        ]
        public string? StartingCoordinate { get; set; }

        #endregion

        [
            DisplayName("Account Group"),
            JsonPropertyName("account_group"),
        ]
        public string? AccountGroup { get; set; }

        [
            DisplayName("Is Event"),
            JsonPropertyName("is_event"),
        ]
        public bool? IsEvent { get; set; }

        [
            DisplayName("Custom Instance Type"),
            JsonPropertyName("custom_instance_type"),
        ]
        public string? CustomInstanceType { get; set; }
    }
}