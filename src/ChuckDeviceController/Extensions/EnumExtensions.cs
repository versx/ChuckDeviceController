namespace ChuckDeviceController.Extensions;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Protos;

public static class EnumExtensions
{
    public static WebhookPayloadType ConvertWebhookType(this WebhookType type)
    {
        return type switch
        {
            WebhookType.Pokemon => WebhookPayloadType.Pokemon,
            WebhookType.Pokestops => WebhookPayloadType.Pokestop,
            WebhookType.Lures => WebhookPayloadType.Lure,
            WebhookType.Invasions => WebhookPayloadType.Invasion,
            WebhookType.Quests => WebhookPayloadType.Quest,
            WebhookType.AlternativeQuests => WebhookPayloadType.AlternativeQuest,
            WebhookType.Gyms => WebhookPayloadType.Gym,
            WebhookType.GymInfo => WebhookPayloadType.GymInfo,
            WebhookType.GymDefenders => WebhookPayloadType.GymDefender,
            WebhookType.GymTrainers => WebhookPayloadType.GymTrainer,
            WebhookType.Eggs => WebhookPayloadType.Egg,
            WebhookType.Raids => WebhookPayloadType.Raid,
            WebhookType.Weather => WebhookPayloadType.Weather,
            WebhookType.Accounts => WebhookPayloadType.Account,
            _ => WebhookPayloadType.Pokemon,
        };
    }
}