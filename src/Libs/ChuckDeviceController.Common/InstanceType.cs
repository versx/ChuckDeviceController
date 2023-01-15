namespace ChuckDeviceController.Common;

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

    #endregion

    #region Constructor

    private InstanceType(string value)
    {
        _value = value;
    }

    #endregion

    #region Overrides

    public static implicit operator InstanceType(string value) => new(value);

    public static implicit operator string(InstanceType value) => value._value;

    public override string ToString() => _value;

    #endregion

    #region Helper Methods

    public static string InstanceTypeToString(InstanceType type) => type.ToString();

    public static InstanceType StringToInstanceType(string instanceType)
    {
        return instanceType switch
        {
            "AutoQuest" => AutoQuest,
            "CirclePokemon" => CirclePokemon,
            "DynamicRoute" => DynamicRoute,
            "CircleRaid" => CircleRaid,
            "SmartRaid" => SmartRaid,
            "PokemonIV" => PokemonIV,
            "Bootstrap" => Bootstrap,
            "FindTth" => FindTth,
            "Leveling" => Leveling,
            "Custom" => Custom,
            _ => CirclePokemon,
        };
    }

    #endregion
}