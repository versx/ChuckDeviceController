namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common.Data.Contracts;

    [Table("device")]
    public class Device : BaseEntity, IDevice
    {
        [
            DisplayName("UUID"),
            Column("uuid"),
            Key,
            JsonPropertyName("uuid"),
        ]
        public string Uuid { get; set; }

        [
            DisplayName("Instance Name"),
            Column("instance_name"),
            JsonPropertyName("instance_name"),
        ]
        public string? InstanceName { get; set; }

        [
            DisplayName("Account Username"),
            Column("account_username"),
            JsonPropertyName("account_username"),
        ]
        public string? AccountUsername { get; set; }

        [
            DisplayName("Last Host"),
            Column("last_host"),
            JsonPropertyName("last_host"),
        ]
        public string? LastHost { get; set; }

        [
            DisplayName("Last Latitude"),
            Column("last_lat"),
            JsonPropertyName("last_lat"),
        ]
        public double? LastLatitude { get; set; }

        [
            DisplayName("Last Longitude"),
            Column("last_lon"),
            JsonPropertyName("last_lon"),
        ]
        public double? LastLongitude { get; set; }

        [
            DisplayName("Last Seen"),
            Column("last_seen"),
            JsonPropertyName("last_seen"),
        ]
        public ulong? LastSeen { get; set; } = 0; // Last job request requested

        // TODO: Add Device LastDataReceived timestamp

        [
            DisplayName("Is Pending Account Switch"),
            Column("pending_account_switch"),
            JsonPropertyName("pending_account_switch"),
        ]
        public bool IsPendingAccountSwitch { get; set; } // used internally

        [
            DisplayName("Last Seen"),
            NotMapped,
        ]
        public string? LastSeenTime { get; set; }

        [
            DisplayName("Online Status"),
            NotMapped,
        ]
        public string? OnlineStatus { get; set; }
    }
}