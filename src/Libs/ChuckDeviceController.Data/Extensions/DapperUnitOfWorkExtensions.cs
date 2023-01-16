namespace ChuckDeviceController.Data.Extensions;

using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Geometry;
using ChuckDeviceController.Geometry.Models.Abstractions;

public static class DapperUnitOfWorkExtensions
{
    public static async Task ClearQuestsAsync(this IDapperUnitOfWork uow)
    {
        var pokestops = await uow.Pokestops.FindAllAsync();
        if (!(pokestops?.Any() ?? false))
        {
            return;
        }

        await uow.ClearQuestsAsync(pokestops);
    }

    public static async Task ClearQuestsAsync(this IDapperUnitOfWork uow, IEnumerable<Pokestop> pokestops)
    {
        var result = await uow.Pokestops.UpdateRangeAsync(pokestops, mappings: new()
        {
            ["id"] = x => x.Id,
            ["quest_conditions"] = x => null!,
            ["quest_rewards"] = x => null!,
            ["quest_target"] = x => null!,
            ["quest_template"] = x => null!,
            ["quest_timestamp"] = x => null!,
            ["quest_title"] = x => null!,
            ["quest_type"] = x => null!,
            ["alternative_quest_conditions"] = x => null!,
            ["alternative_quest_rewards"] = x => null!,
            ["alternative_quest_target"] = x => null!,
            ["alternative_quest_template"] = x => null!,
            ["alternative_quest_timestamp"] = x => null!,
            ["alternative_quest_title"] = x => null!,
            ["alternative_quest_type"] = x => null!,
        });
        var pokestopIds = string.Join(", ", pokestops.Select(x => x.Id).ToList());
        Console.WriteLine($"Clear Quests Result: {result}\nPokestop IDs: {pokestopIds}");
        //_logger.LogInformation($"[{Name}] Clear Quests Result: {Result}", Name, result);
    }


    /// <summary>
    /// Clear all Pokestop quests within geofences
    /// </summary>
    /// <param name="context"></param>
    /// <param name="multiPolygons"></param>
    public static async Task ClearQuestsAsync(this IDapperUnitOfWork uow, IEnumerable<IMultiPolygon> multiPolygons)
    {
        foreach (var multiPolygon in multiPolygons)
        {
            await uow.ClearQuestsAsync(multiPolygon);
        }
    }

    /// <summary>
    /// Clear all Pokestop quests within geofence
    /// </summary>
    /// <param name="context"></param>
    /// <param name="multiPolygon"></param>
    public static async Task ClearQuestsAsync(this IDapperUnitOfWork uow, IMultiPolygon multiPolygon)
    {
        var bbox = multiPolygon.GetBoundingBox();
        var pokestops = await uow.Pokestops.FindAsync(stop =>
            stop.Latitude >= bbox.MinimumLatitude &&
            stop.Longitude >= bbox.MinimumLongitude &&
            stop.Latitude <= bbox.MaximumLatitude &&
            stop.Longitude <= bbox.MaximumLongitude
        );
        var pokestopInGeofence = pokestops
            .Where(stop => GeofenceService.InPolygon(multiPolygon, stop.ToCoordinate()));
        await uow.ClearQuestsAsync(pokestopInGeofence);
    }

    /// <summary>
    /// Clear all Pokestop quests within geofence
    /// </summary>
    /// <param name="context"></param>
    /// <param name="geofences"></param>
    /// <returns></returns>
    public static async Task ClearQuestsAsync(this IDapperUnitOfWork uow, IReadOnlyList<Geofence> geofences)
    {
        var (multiPolygons, _) = geofences.ConvertToMultiPolygons();
        await uow.ClearQuestsAsync(multiPolygons);
    }



    public static async Task<ulong> GetQuestCountAsync(this IDapperUnitOfWork uow, List<string> pokestopIds, QuestMode mode, uint maxBatchCount = 10000)
    {
        if (pokestopIds.Count > maxBatchCount)
        {
            var result = 0ul;
            var batchSize = Convert.ToInt64(Math.Ceiling(Convert.ToDouble(pokestopIds.Count) / maxBatchCount));
            for (var i = 0; i < batchSize; i++)
            {
                var start = maxBatchCount * i;
                var end = Math.Max(maxBatchCount * i, pokestopIds.Count - 1);
                var splice = pokestopIds.GetRange((int)start, (int)end);
                var spliceResult = await uow.GetQuestCountAsync(splice, mode);
                result += spliceResult;
            }
            return result;
        }

        var pokestops = await uow.GetPokestopsByIdsAsync(pokestopIds);
        var count = pokestops.Count(stop => HasPokestopQuestByType(stop, mode));
        return Convert.ToUInt64(count);
    }

    public static async Task<IEnumerable<Pokestop>> GetPokestopsByIdsAsync(this IDapperUnitOfWork uow, IEnumerable<string> pokestopIds, bool isEnabled = true, bool isDeleted = false)
    {
        var pokestops = await uow.Pokestops.FindAsync(pokestop =>
            pokestopIds.Contains(pokestop.Id) &&
            isEnabled == pokestop.IsEnabled &&
            isDeleted == pokestop.IsDeleted
        );
        return pokestops;
    }

    public static async Task<IEnumerable<Pokestop>> GetPokestopsInBoundsAsync(this IDapperUnitOfWork uow, IBoundingBox bbox, bool isEnabled = true)
    {
        var pokestops = await uow.Pokestops.FindAsync(pokestop =>
            pokestop.Latitude >= bbox.MinimumLatitude &&
            pokestop.Longitude >= bbox.MinimumLongitude &&
            pokestop.Latitude <= bbox.MaximumLatitude &&
            pokestop.Longitude <= bbox.MaximumLongitude &&
            isEnabled == pokestop.IsEnabled &&
            !pokestop.IsDeleted
        );
        return pokestops;
    }

    public static bool HasPokestopQuestByType(this Pokestop pokestop, QuestMode mode)
    {
        var result = mode == QuestMode.Normal
            // Return only normal quest types
            ? pokestop.QuestType != null
            : mode == QuestMode.Alternative
                // Return only alternative quest types
                ? pokestop.AlternativeQuestType != null
                // Return normal and alternative quest types
                : pokestop.QuestType != null || pokestop.AlternativeQuestType != null;
        return result;
    }

    /// <summary>
    /// Get all S2 cells within bounding box
    /// </summary>
    /// <param name="context"></param>
    /// <param name="bbox"></param>
    public static async Task<IEnumerable<Cell>> GetS2CellsAsync(this IDapperUnitOfWork uow, IBoundingBox bbox)
    {
        var cells = await uow.Cells.FindAsync(cell =>
            cell.Latitude >= bbox.MinimumLatitude &&
            cell.Longitude >= bbox.MinimumLongitude &&
            cell.Latitude <= bbox.MaximumLatitude &&
            cell.Longitude <= bbox.MaximumLongitude
        );
        return cells;
    }

    /// <summary>
    /// Get all S2 cells within geofence polygon
    /// </summary>
    /// <param name="context"></param>
    /// <param name="multiPolygon"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<Cell>> GetS2CellsAsync(this IDapperUnitOfWork uow, IMultiPolygon multiPolygon)
    {
        var bbox = multiPolygon.GetBoundingBox();
        // Get S2 cells within geofence bounding box
        var bboxCells = await GetS2CellsAsync(uow, bbox);
        // Filter S2 cells outside of geofence polygon
        var cellsInArea = bboxCells
            .Where(cell => GeofenceService.InPolygon(multiPolygon, cell.ToCoordinate()))
            .ToList();
        return cellsInArea;
    }

    /// <summary>
    /// Get all S2 cells within geofence polygons
    /// </summary>
    /// <param name="context"></param>
    /// <param name="multiPolygons"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<Cell>> GetS2CellsAsync(this IDapperUnitOfWork uow, IEnumerable<IMultiPolygon> multiPolygons)
    {
        var cells = multiPolygons
            .SelectMany(multiPolygon => GetS2CellsAsync(uow, multiPolygon).Result)
            .ToList();
        //foreach (var multiPolygon in multiPolygons)
        //{
        //    var list = await GetS2CellsAsync(context, multiPolygon);
        //    cells.AddRange(list);
        //}
        return await Task.FromResult(cells);
    }
}