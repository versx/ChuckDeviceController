namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    public class InstanceData
    {
        [
            DisplayName("Circle Instance Route Type"),
            JsonPropertyName("circle_route_type"),
        ]
        public CircleInstanceRouteType? CircleRouteType { get; set; }

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
        public string? QuestMode { get; set; }

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

        public InstanceData()
        {
            CircleRouteType = CircleInstanceRouteType.Default;
        }
    }
}