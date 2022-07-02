namespace ChuckDeviceController.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WebhookType
    {
        Pokemon = 0,
        Pokestops,
        Raids,
        Eggs,
        Quests,
        Lures,
        Invasions,
        Gyms,
        Weather,
    }
}