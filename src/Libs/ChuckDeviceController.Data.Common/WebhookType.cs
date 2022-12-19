namespace ChuckDeviceController.Data.Common;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WebhookType
{
    Pokemon = 0,
    Pokestops,
    Lures,
    Invasions,
    Quests,
    AlternativeQuests,
    Gyms,
    GymInfo,
    GymDefenders,
    GymTrainers,
    Eggs,
    Raids,
    Weather,
    Accounts,
}