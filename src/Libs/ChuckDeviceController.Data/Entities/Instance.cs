namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Abstractions;
using ChuckDeviceController.Extensions.Json.Converters;

[Table("instance")]
public class Instance : BaseEntity, IInstance
{
    #region Properties

    [
        DisplayName("Name"),
        Column("name"),
        Key,
        JsonPropertyName("name"),
    ]
    public string Name { get; set; } = null!;

    [
        DisplayName("Instance Type"),
        Column("type"),
        Required,
        JsonPropertyName("type"),
    ]
    public InstanceType Type { get; set; }

    [
        DisplayName("Minimum Level"),
        Column("min_level"),
        Required,
        Range(0, 50),
        JsonPropertyName("min_level"),
    ]
    public ushort MinimumLevel { get; set; }

    [
        DisplayName("Maximum Level"),
        Column("max_level"),
        Required,
        Range(0, 50),
        JsonPropertyName("max_level"),
    ]
    public ushort MaximumLevel { get; set; }

    [
        DisplayName("Geofences"),
        Column("geofences"),
        Required,
        JsonPropertyName("geofences"),
    ]
    public List<string> Geofences { get; set; } = new();

    [
        DisplayName("Data"),
        Column("data"),
        JsonPropertyName("data"),
        //JsonExtensionData,
        JsonConverter(typeof(ObjectDataConverter<InstanceData>)),
    ]
    public InstanceData? Data { get; set; }

    [
        DisplayName("No. Devices"),
        NotMapped,
        JsonIgnore,
    ]
    public string? DeviceCount { get; set; }

    [
        DisplayName("Status"),
        NotMapped,
        JsonIgnore,
    ]
    public string? Status { get; set; }

    [JsonIgnore]
    public virtual ICollection<Device>? Devices { get; set; }

    #endregion
}