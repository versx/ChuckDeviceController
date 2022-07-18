namespace ChuckDeviceController.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InstanceType
    {
        CirclePokemon,
        CircleSmartPokemon,
        DynamicPokemon,
        CircleRaid,
        CircleSmartRaid, // TODO: Rename to SmartRaid
        AutoQuest,
        PokemonIV,
        Bootstrap,
        FindTth,
    }
}