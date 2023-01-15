namespace ChuckDeviceCommunicator.Extensions;

using System.Text;

using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Geometry;

public static class WebhookProcessingExtensions
{
    public static IEnumerable<dynamic> ProcessPokemon(this Webhook endpoint, IReadOnlyDictionary<string, Pokemon> pokemonEvents)
    {
        foreach (var (_, pokemon) in pokemonEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(pokemon.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.PokemonIds?.Any() ?? false)
            {
                if (IsPokemonBlacklisted(
                    pokemon.PokemonId,
                    pokemon.Form,
                    pokemon.Costume,
                    pokemon.Gender,
                    endpoint.Data.PokemonIds
                ))
                    continue;
            }
            var data = pokemon.GetWebhookData(WebhookHeaders.Pokemon);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessPokestops(this Webhook endpoint, IReadOnlyDictionary<string, Pokestop> pokestopEvents)
    {
        foreach (var (_, pokestop) in pokestopEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(pokestop.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.PokestopIds?.Any() ?? false)
            {
                if (endpoint.Data.PokestopIds.Contains(pokestop.Id))
                    continue;
            }
            var data = pokestop.GetWebhookData(WebhookHeaders.Pokestop);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessLures(this Webhook endpoint, IReadOnlyDictionary<string, Pokestop> lureEvents)
    {
        foreach (var (_, lure) in lureEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(lure.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.LureIds?.Any() ?? false)
            {
                if (endpoint.Data.LureIds.Contains(lure.LureId))
                    continue;
            }
            var data = lure.GetWebhookData(WebhookHeaders.Lure);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessInvasions(this Webhook endpoint, IReadOnlyDictionary<string, PokestopWithIncident> invasionEvents)
    {
        foreach (var (_, pokestopWithIncident) in invasionEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(pokestopWithIncident.Pokestop.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.InvasionIds?.Any() ?? false)
            {
                if (endpoint.Data.InvasionIds.Contains(pokestopWithIncident.Invasion.Character))
                    continue;
            }
            var data = pokestopWithIncident.Invasion.GetWebhookData(WebhookHeaders.Invasion, pokestopWithIncident.Pokestop);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessQuests(this Webhook endpoint, IReadOnlyDictionary<string, Pokestop> questEvents)
    {
        foreach (var (_, quest) in questEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(quest.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            // TODO: Add quest filtering
            //if (endpoint.Data?.Quests.Any() ?? false)
            //{
            //}
            var data = quest.GetWebhookData(WebhookHeaders.Quest);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessAltQuests(this Webhook endpoint, IReadOnlyDictionary<string, Pokestop> alternativeQuestEvents)
    {
        foreach (var (_, alternativeQuest) in alternativeQuestEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(alternativeQuest.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            //if (endpoint.Data?.Quests.Any() ?? false)
            //{
            //}
            var data = alternativeQuest.GetWebhookData(WebhookHeaders.AlternativeQuest);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessGyms(this Webhook endpoint, IReadOnlyDictionary<string, Gym> gymEvents)
    {
        foreach (var (_, gym) in gymEvents)
        {
            if ((endpoint.Geofences?.Count ?? 0) > 0)
            {
                if (!GeofenceService.IsPointInPolygon(gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.GymTeamIds?.Any() ?? false)
            {
                if (endpoint.Data.GymTeamIds.Contains((ushort)gym.Team))
                    continue;
            }
            var data = gym.GetWebhookData(WebhookHeaders.Gym);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessGymInfo(this Webhook endpoint, IReadOnlyDictionary<string, Gym> gymInfoEvents)
    {
        foreach (var (_, gymInfo) in gymInfoEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(gymInfo.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.GymIds?.Any() ?? false)
            {
                if (endpoint.Data.GymIds.Contains(gymInfo.Id))
                    continue;
            }
            var data = gymInfo.GetWebhookData("gym-info");
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessGymDefenders(this Webhook endpoint, IReadOnlyDictionary<ulong, GymWithDefender> gymDefenderEvents)
    {
        foreach (var (_, gymDefender) in gymDefenderEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(gymDefender.Gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            // TODO: Add gym defenders filtering
            var data = gymDefender.Defender.GetWebhookData("gym-defender", gymDefender.Gym);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessGymTrainers(this Webhook endpoint, IReadOnlyDictionary<string, GymWithTrainer> gymTrainerEvents)
    {
        foreach (var (_, gymTrainer) in gymTrainerEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(gymTrainer.Gym.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            // TODO: Add gym trainers filtering
            var data = gymTrainer.Trainer.GetWebhookData("gym-trainer", gymTrainer.Gym);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessEggs(this Webhook endpoint, IReadOnlyDictionary<string, Gym> eggEvents)
    {
        foreach (var (_, egg) in eggEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(egg.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.EggLevels?.Any() ?? false)
            {
                if (endpoint.Data.EggLevels.Contains(egg.RaidLevel ?? 0))
                    continue;
            }
            var data = egg.GetWebhookData(WebhookHeaders.Egg);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessRaids(this Webhook endpoint, IReadOnlyDictionary<string, Gym> raidEvents)
    {
        foreach (var (_, raid) in raidEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(raid.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.RaidPokemonIds?.Any() ?? false)
            {
                if (IsPokemonBlacklisted(
                    raid.RaidPokemonId,
                    raid.RaidPokemonForm,
                    raid.RaidPokemonCostume,
                    raid.RaidPokemonGender,
                    endpoint.Data.PokemonIds
                ))
                    continue;
            }
            var data = raid.GetWebhookData(WebhookHeaders.Raid);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessWeather(this Webhook endpoint, IReadOnlyDictionary<long, Weather> weatherEvents)
    {
        foreach (var (_, weather) in weatherEvents)
        {
            if (endpoint.Geofences?.Any() ?? false)
            {
                if (!GeofenceService.IsPointInPolygon(weather.ToCoordinate(), endpoint.GeofenceMultiPolygons))
                    continue;
            }
            if (endpoint.Data?.WeatherConditionIds?.Any() ?? false)
            {
                if (endpoint.Data.WeatherConditionIds.Contains((ushort)weather.GameplayCondition))
                    continue;
            }
            var data = weather.GetWebhookData(WebhookHeaders.Weather);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public static IEnumerable<dynamic> ProcessAccounts(this Webhook endpoint, IReadOnlyDictionary<string, Account> accountEvents)
    {
        foreach (var (_, account) in accountEvents)
        {
            var data = account.GetWebhookData(WebhookHeaders.Account);
            // TODO: Ignore list of account names, account statuses, and/or account levels
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private static bool IsPokemonBlacklisted(uint? pokemonId, uint? formId, uint? costumeId, ushort? genderId, IEnumerable<string>? blacklisted = null)
    {
        if (!(blacklisted?.Any() ?? false))
            return false;

        var sb = new StringBuilder();
        sb.Append($"{pokemonId}");
        if (formId > 0) sb.Append($"_f{formId}");
        if (costumeId > 0) sb.Append($"_c{costumeId}");
        if (genderId > 0) sb.Append($"_g{genderId}");

        var key = sb.ToString();
        var matches = blacklisted.Contains(key);
        return matches;
    }
}