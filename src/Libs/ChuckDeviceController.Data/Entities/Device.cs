namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceController.Data.Abstractions;

[Table("device")]
public class Device : BaseEntity, IDevice
{
    [
        DisplayName("UUID"),
        Column("uuid"),
        Key,
        JsonPropertyName("uuid"),
    ]
    public string Uuid { get; set; } = null!;

    [
        DisplayName("Instance Name"),
        Column("instance_name"),
        ForeignKey("instance_name"),
        JsonPropertyName("instance_name"),
    ]
    public string? InstanceName { get; set; }

    [JsonIgnore]
    public virtual Instance? Instance { get; set; }

    [
        DisplayName("Account Username"),
        Column("account_username"),
        ForeignKey("account_username"),
        JsonPropertyName("account_username"),
    ]
    public string? AccountUsername { get; set; }

    [JsonIgnore]
    public virtual Account? Account { get; set; }

    [
        DisplayName("Level"),
        NotMapped,
        JsonPropertyName("account_level"),
    ]
    public ushort AccountLevel { get; set; }

    [
        DisplayName("Last Host"),
        Column("last_host"),
        JsonPropertyName("last_host"),
    ]
    public string? LastHost { get; set; }

    [
        DisplayName("Last Latitude"),
        Column("last_lat"),
        Precision(18, 6),
        JsonPropertyName("last_lat"),
    ]
    public double? LastLatitude { get; set; }

    [
        DisplayName("Last Longitude"),
        Column("last_lon"),
        Precision(18, 6),
        JsonPropertyName("last_lon"),
    ]
    public double? LastLongitude { get; set; }

    [
        DisplayName("Last Seen"),
        Column("last_seen"),
        JsonPropertyName("last_seen"),
    ]
    public ulong? LastSeen { get; set; } // Last job request requested

    [
        DisplayName("Is Pending Account Switch"),
        Column("pending_account_switch"),
        JsonPropertyName("pending_account_switch"),
    ]
    public bool IsPendingAccountSwitch { get; set; } // Used internally
}