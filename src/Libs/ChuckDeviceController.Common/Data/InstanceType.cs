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
}