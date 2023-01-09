namespace ChuckDeviceController.Data.Extensions;

using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Geometry;
using ChuckDeviceController.Geometry.Models.Abstractions;

public static class MapDataContextExtensions
{
    #region Clear Quests

    /// <summary>
    /// Clear all Pokestops with quests
    /// </summary>
    /// <param name="context"></param>
    public static async Task ClearQuestsAsync(this MapDbContext context)
    {
        var pokestops = context.Pokestops.ToList();
        await ClearQuestsAsync(context, pokestops);
    }

    /// <summary>
    /// Clear all Pokestops with quests
    /// </summary>
    /// <param name="context"></param>
    /// <param name="pokestops"></param>
    /// <returns></returns>
    public static async Task ClearQuestsAsync(this MapDbContext context, List<Pokestop> pokestops)
    {
        pokestops.ForEach(pokestop =>
        {
            pokestop.QuestConditions = null;
            pokestop.QuestRewards = null;
            pokestop.QuestTarget = null;
            pokestop.QuestTemplate = null;
            pokestop.QuestTimestamp = null;
            pokestop.QuestTitle = null;
            pokestop.QuestType = null;

            pokestop.AlternativeQuestConditions = null;
            pokestop.AlternativeQuestRewards = null;
            pokestop.AlternativeQuestTarget = null;
            pokestop.AlternativeQuestTemplate = null;
            pokestop.AlternativeQuestTimestamp = null;
            pokestop.AlternativeQuestTitle = null;
            pokestop.AlternativeQuestType = null;
        });

        context.Pokestops.UpdateRange(pokestops);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Clear all Pokestops with quests by Pokestop IDs
    /// </summary>
    /// <param name="context"></param>
    /// <param name="pokestopIds"></param>
    public static async Task ClearQuestsAsync(this MapDbContext context, IEnumerable<string> pokestopIds)
    {
        var pokestops = GetPokestopsByIds(context, pokestopIds);
        await ClearQuestsAsync(context, pokestops);
    }

    /// <summary>
    /// Clear all Pokestop quests within geofences
    /// </summary>
    /// <param name="context"></param>
    /// <param name="multiPolygons"></param>
    public static async Task ClearQuestsAsync(this MapDbContext context, IEnumerable<IMultiPolygon> multiPolygons)
    {
        foreach (var multiPolygon in multiPolygons)
        {
            await ClearQuestsAsync(context, multiPolygon);
        }
    }

    /// <summary>
    /// Clear all Pokestop quests within geofence
    /// </summary>
    /// <param name="context"></param>
    /// <param name="multiPolygon"></param>
    public static async Task ClearQuestsAsync(this MapDbContext context, IMultiPolygon multiPolygon)
    {
        var bbox = multiPolygon.GetBoundingBox();
        var pokestops = context.Pokestops.Where(stop =>
            stop.Latitude >= bbox.MinimumLatitude &&
            stop.Longitude >= bbox.MinimumLongitude &&
            stop.Latitude <= bbox.MaximumLatitude &&
            stop.Longitude <= bbox.MaximumLongitude
        ).ToList();
        var pokestopIds = pokestops
            .Where(stop => GeofenceService.InPolygon(multiPolygon, stop.ToCoordinate()))
            .Select(stop => stop.Id);
        await ClearQuestsAsync(context, pokestopIds);
    }

    /// <summary>
    /// Clear all Pokestop quests within geofence
    /// </summary>
    /// <param name="context"></param>
    /// <param name="geofences"></param>
    /// <returns></returns>
    public static async Task ClearQuestsAsync(this MapDbContext context, IReadOnlyList<Geofence> geofences)
    {
        var (multiPolygons, _) = geofences.ConvertToMultiPolygons();
        await ClearQuestsAsync(context, multiPolygons);
    }

    #endregion

    #region Pokestops

    public static List<Pokestop> GetPokestopsByIds(this MapDbContext context, IEnumerable<string> pokestopIds, bool isEnabled = true, bool isDeleted = false)
    {
        var pokestops = context.Pokestops
            .Where(pokestop => pokestopIds.Contains(pokestop.Id))
            .Where(pokestop => isEnabled == pokestop.IsEnabled)
            .Where(pokestop => isDeleted == pokestop.IsDeleted)
            .ToList();
        return pokestops;
    }

    #endregion
}