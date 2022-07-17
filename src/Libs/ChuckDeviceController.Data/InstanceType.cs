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
        CircleSmartRaid,
        AutoQuest,
        PokemonIV,
        Bootstrap,
        FindTth,
    }
}