namespace ChuckDeviceController.Common.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InstanceType
    {
        CirclePokemon,
        CircleSmartPokemon, // TODO: redundant remove eventually
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
}