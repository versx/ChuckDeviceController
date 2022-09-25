namespace ChuckDeviceController.Common.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InstanceType
    {
        CirclePokemon,
        DynamicRoute,
        CircleRaid,
        SmartRaid,
        AutoQuest,
        PokemonIV,
        Bootstrap,
        FindTth,
        Leveling,
        Custom,
    }

    public static class InstanceDescriptors
    {
        public static readonly IReadOnlyDictionary<InstanceType, string> TypeDescriptions
            = new Dictionary<InstanceType, string>
        {
            { InstanceType.CirclePokemon, "Plotted circles to find Pokemon spawns." },
            { InstanceType.DynamicRoute, "Dynamically generated routing for raids and Pokemon spawns." },
            { InstanceType.CircleRaid, "Plotted circles to find raids." },
            { InstanceType.SmartRaid, "Smart raid scanner which calculates when to check times." },
            { InstanceType.AutoQuest, "Pokestop field research quest scanner." },
            { InstanceType.PokemonIV, "Rare Pokemon spawns priority list based scanner." },
            { InstanceType.Bootstrap, "Quickly scan at area based on custom circle plot sizes." },
            { InstanceType.FindTth, "Unknown spawnpoint scanner and monitor." },
            { InstanceType.Leveling, "Trainer account level increaser." },
            { InstanceType.Custom, "Custom scanner type provided by a plugin." },
        };
    }
}