namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    [Table("device")]
    public class Device : BaseEntity
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
        public ulong? LastSeen { get; set; } = 0; // Last job request received

        // TODO: Last received data

        [

            DisplayName("Last Seen"),
            NotMapped,
        ]
        public string LastSeenTime { get; set; }
    }
}