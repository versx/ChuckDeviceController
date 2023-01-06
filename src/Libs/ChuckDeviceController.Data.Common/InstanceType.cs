namespace ChuckDeviceController.Data.Common;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstanceType
{
    [Display(Name = "Circle Pokemon", Description = "Plotted circles to find Pokemon spawns.")]
    CirclePokemon,

    [Display(Name = "Dynamic Routing", Description = "Dynamically generated routing for raids and Pokemon spawns.")]
    DynamicRoute,

    [Display(Name = "Circle Raid", Description = "Plotted circles to find raids.")]
    CircleRaid,

    [Display(Name = "Smart Raid", Description = "Smart raid scanner which calculates when to check times.")]
    SmartRaid,

    [Display(Name = "Auto-Quest", Description = "Pokestop field research quest scanner.")]
    AutoQuest,

    [Display(Name = "Pokemon IV", Description = "Rare Pokemon spawns priority list based scanner.")]
    PokemonIV,

    [Display(Name = "Bootstrapper", Description = "Quickly scan at area based on custom circle plot sizes.")]
    Bootstrap,

    [Display(Name = "Find Tth", Description = "Unknown spawnpoint scanner and monitor.")]
    FindTth,

    [Display(Name = "Leveling", Description = "Trainer account level increaser.")]
    Leveling,

    [Display(Name = "Custom Job Controller", Description = "Custom scanner type provided by a plugin.")]
    Custom,
}

public static class InstanceDescriptors
{
    public static readonly IReadOnlyDictionary<InstanceType, string> TypeDescriptions
        = new Dictionary<InstanceType, string>
    {
        [InstanceType.CirclePokemon] = "Plotted circles to find Pokemon spawns.",
        [InstanceType.DynamicRoute] = "Dynamically generated routing for raids and Pokemon spawns.",
        [InstanceType.CircleRaid] = "Plotted circles to find raids.",
        [InstanceType.SmartRaid] = "Smart raid scanner which calculates when to check times.",
        [InstanceType.AutoQuest] = "Pokestop field research quest scanner.",
        [InstanceType.PokemonIV] = "Rare Pokemon spawns priority list based scanner.",
        [InstanceType.Bootstrap] = "Quickly scan at area based on custom circle plot sizes.",
        [InstanceType.FindTth] = "Unknown spawnpoint scanner and monitor.",
        [InstanceType.Leveling] = "Trainer account level increaser.",
        [InstanceType.Custom] = "Custom scanner type provided by a plugin.",
    };
}