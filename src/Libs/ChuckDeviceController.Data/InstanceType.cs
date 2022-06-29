namespace ChuckDeviceController.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InstanceType
    {
        CirclePokemon,
        CircleSmartPokemon,
        CircleRaid,
        CircleSmartRaid,
        AutoQuest,
        PokemonIV,
        Bootstrap,
        FindTth,
    }
}