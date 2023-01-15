namespace ChuckDeviceController.Common;

//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//[JsonConverter(typeof(JsonStringEnumConverter))]
//public enum InstanceType
//{
//    [Display(Name = "Circle Pokemon", Description = "Plotted circles to find Pokemon spawns.")]
//    CirclePokemon,

//    [Display(Name = "Dynamic Routing", Description = "Dynamically generated routing for raids and Pokemon spawns.")]
//    DynamicRoute,

//    [Display(Name = "Circle Raid", Description = "Plotted circles to find raids.")]
//    CircleRaid,

//    [Display(Name = "Smart Raid", Description = "Smart raid scanner which calculates when to check times.")]
//    SmartRaid,

//    [Display(Name = "Auto-Quest", Description = "Pokestop field research quest scanner.")]
//    AutoQuest,

//    [Display(Name = "Pokemon IV", Description = "Rare Pokemon spawns priority list based scanner.")]
//    PokemonIV,

//    [Display(Name = "Bootstrapper", Description = "Quickly scan at area based on custom circle plot sizes.")]
//    Bootstrap,

//    [Display(Name = "Find Tth", Description = "Unknown spawnpoint scanner and monitor.")]
//    FindTth,

//    [Display(Name = "Leveling", Description = "Trainer account level increaser.")]
//    Leveling,

//    [Display(Name = "Custom Job Controller", Description = "Custom scanner type provided by a plugin.")]
//    Custom,
//}

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

public readonly struct InstanceType
{
    private readonly string _value;

    #region Properties

    public static IEnumerable<InstanceType> Values => new[]
    {
        CirclePokemon,
        CircleRaid,
        DynamicRoute,
        SmartRaid,
        AutoQuest,
        PokemonIV,
        Bootstrap,
        FindTth,
        Leveling,
        Custom,
    };

    public static InstanceType CirclePokemon => "CirclePokemon";

    public static InstanceType DynamicRoute => "DynamicRoute";

    public static InstanceType CircleRaid => "CircleRaid";

    public static InstanceType SmartRaid => "SmartRaid";

    public static InstanceType AutoQuest => "AutoQuest";

    public static InstanceType PokemonIV => "PokemonIV";

    public static InstanceType Bootstrap => "Bootstrap";

    public static InstanceType FindTth => "FindTth";

    public static InstanceType Leveling => "Leveling";

    public static InstanceType Custom => "Custom";

    //public static InstanceType CirclePokemon => "circle_pokemon";

    //public static InstanceType DynamicRoute => "dynamic_route";

    //public static InstanceType CircleRaid => "circle_raid";

    //public static InstanceType SmartRaid => "smart_raid";

    //public static InstanceType AutoQuest => "auto_quest";

    //public static InstanceType PokemonIV => "pokemon_iv";

    //public static InstanceType Bootstrap => "bootstrap";

    //public static InstanceType FindTth => "find_tth";

    //public static InstanceType Leveling => "leveling";

    //public static InstanceType Custom => "custom";

    #endregion

    #region Constructor

    private InstanceType(string value)
    {
        _value = value;
    }

    #endregion

    #region Overrides

    public static implicit operator InstanceType(string value)
    {
        return new InstanceType(value);
    }

    public static implicit operator string(InstanceType value)
    {
        return value._value;
    }

    public override string ToString()
    {
        return _value;
    }

    #endregion
}