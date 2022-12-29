namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
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
        JsonPropertyName("min_level"),
    ]
    public ushort MinimumLevel { get; set; }

    [
        DisplayName("Maximum Level"),
        Column("max_level"),
        Required,
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

    #endregion

    #region Helper Methods

    public static string InstanceTypeToString(InstanceType type)
    {
        return type switch
        {
            InstanceType.AutoQuest => "auto_quest",
            InstanceType.CirclePokemon => "circle_pokemon",
            InstanceType.DynamicRoute => "dynamic_route",
            InstanceType.CircleRaid => "circle_raid",
            InstanceType.SmartRaid => "smart_raid",
            InstanceType.PokemonIV => "pokemon_iv",
            InstanceType.Bootstrap => "bootstrap",
            InstanceType.FindTth => "find_tth",
            InstanceType.Leveling => "leveling",
            InstanceType.Custom => "custom",
            _ => type.ToString(),
        };
    }

    public static InstanceType StringToInstanceType(string instanceType)
    {
        return (instanceType.ToLower()) switch
        {
            "auto_quest" => InstanceType.AutoQuest,
            "circle_pokemon" => InstanceType.CirclePokemon,
            "dynamic_route" => InstanceType.DynamicRoute,
            "circle_raid" => InstanceType.CircleRaid,
            "smart_raid" => InstanceType.SmartRaid,
            "pokemon_iv" => InstanceType.PokemonIV,
            "bootstrap" => InstanceType.Bootstrap,
            "find_tth" => InstanceType.FindTth,
            "leveling" => InstanceType.Leveling,
            "custom" => InstanceType.Custom,
            _ => InstanceType.CirclePokemon,
        };
    }

    #endregion
}