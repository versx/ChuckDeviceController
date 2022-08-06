﻿namespace ChuckDeviceController.Common.Data
{
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
        Eggs,
        Raids,
        Weather,
        Accounts,
    }
}