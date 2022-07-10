namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    using ChuckDeviceController.Data;

    public class EditInstanceViewModel
    {
        [DisplayName("Name")]
        public string Name { get; set; }

        [DisplayName("Instance Type")]
        public InstanceType Type { get; set; }

        [DisplayName("Minimum Level")]
        public ushort MinimumLevel { get; set; }

        [DisplayName("Maximum Level")]
        public ushort MaximumLevel { get; set; }

        [DisplayName("Geofences")]
        public List<string> Geofences { get; set; } = new();

        [DisplayName("Instance Data")]
        public EditInstanceDataViewModel Data { get; set; } = new();
    }

    public class EditInstanceDataViewModel
    {
        // Circle Instance
        [DisplayName("Circle Instance Route Type")]
        public CircleInstanceRouteType? CircleRouteType { get; set; }


        // Quest Instance
        [DisplayName("Time Zone")]
        public string? TimeZone { get; set; }

        [DisplayName("Enable DST")]
        public bool EnableDst { get; set; }

        [DisplayName("Spin Limit")]
        public ushort? SpinLimit { get; set; }

        [DisplayName("Ignore S2 Cell Bootstrapping")]
        public bool IgnoreS2CellBootstrap { get; set; }

        [DisplayName("Use Red Warning Accounts")]
        public bool UseWarningAccounts { get; set; }

        [DisplayName("Quest Mode")]
        public QuestMode? QuestMode { get; set; }


        // IV Instance
        [DisplayName("IV Queue Limit")]
        public ushort? IvQueueLimit { get; set; }

        [DisplayName("IV List")]
        public string? IvList { get; set; }

        [DisplayName("Enable Lure Encounters")]
        public bool EnableLureEncounters { get; set; }


        // Bootstrap Instance
        [DisplayName("Fast Bootstrap Mode")]
        public bool FastBootstrapMode { get; set; }

        [DisplayName("Circle Size")]
        public ushort? CircleSize { get; set; }


        // All
        [DisplayName("Account Group")]
        public string? AccountGroup { get; set; }

        [DisplayName("Is Event")]
        public bool IsEvent { get; set; }
    }
}
