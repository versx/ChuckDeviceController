namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    [Table("device")]
    public class Device : BaseEntity
    {
        [
            Column("uuid"),
            Key,
            JsonPropertyName("uuid"),
        ]
        public string Uuid { get; set; }

        [
            Column("instance_name"),
            JsonPropertyName("instance_name"),
        ]
        public string InstanceName { get; set; }

        [
            Column("account_username"),
            JsonPropertyName("account_username"),
        ]
        public string AccountUsername { get; set; }

        [
            Column("last_host"),
            JsonPropertyName("last_host"),
        ]
        public string LastHost { get; set; }

        [
            Column("last_lat"),
            JsonPropertyName("last_lat"),
        ]
        public double? LastLatitude { get; set; }

        [
            Column("last_lon"),
            JsonPropertyName("last_lon"),
        ]
        public double? LastLongitude { get; set; }

        [
            Column("last_seen"),
            JsonPropertyName("last_seen"),
        ]
        public ulong? LastSeen { get; set; } = 0;
    }
}