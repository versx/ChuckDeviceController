namespace ChuckDeviceController.Data.Extensions
{

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Geometry;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry;

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
            await context.Pokestops.BulkUpdateAsync(pokestops, options =>
            {
                options.TemporaryTableUseTableLock = true;
                options.UseTableLock = true;
                options.ColumnPrimaryKeyExpression = p => p.Id;
                options.ColumnInputExpression = p => new
                {
                    p.Id,

                    p.QuestConditions,
                    p.QuestRewards,
                    p.QuestTarget,
                    p.QuestTemplate,
                    p.QuestTimestamp,
                    p.QuestTitle,
                    p.QuestType,

                    p.AlternativeQuestConditions,
                    p.AlternativeQuestRewards,
                    p.AlternativeQuestTarget,
                    p.AlternativeQuestTemplate,
                    p.AlternativeQuestTimestamp,
                    p.AlternativeQuestTitle,
                    p.AlternativeQuestType,
                };
            });
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
            var pokestopIds = pokestops.Where(stop => GeofenceService.InPolygon(multiPolygon, stop.ToCoordinate()))
                                       .Select(stop => stop.Id);
            await ClearQuestsAsync(context, pokestopIds);
        }

        /// <summary>
        /// Clear all Pokestop quests within bounding box
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bbox"></param>
        public static async Task ClearQuestsAsync(this MapDbContext context, IBoundingBox bbox)
        {
            var pokestopIds = context.Pokestops.Where(stop =>
                stop.Latitude >= bbox.MinimumLatitude &&
                stop.Longitude >= bbox.MinimumLongitude &&
                stop.Latitude <= bbox.MaximumLatitude &&
                stop.Longitude <= bbox.MaximumLongitude
            )
                .Select(stop => stop.Id)
                .ToList();
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

        public static async Task<ulong> GetPokestopQuestCountAsync(this MapDbContext context, List<string> pokestopIds, QuestMode mode)
        {
            if (pokestopIds.Count > 10000)
            {
                // TODO: Benchmark if manual batching is necessary with Z.EntityFramework library (which already has it's own batching options/logic)
                var result = 0ul;
                var batchSize = Convert.ToInt64(Math.Ceiling(Convert.ToDouble(pokestopIds.Count) / 10000.0));
                for (var i = 0; i < batchSize; i++)
                {
                    var start = 10000 * i;
                    var end = Math.Max(10000 * i, pokestopIds.Count - 1);
                    var splice = pokestopIds.GetRange(start, end);
                    var spliceResult = await GetPokestopQuestCountAsync(context, splice, mode);
                    result += spliceResult;
                }
                return result;
            }

            var pokestops = GetPokestopsByIds(context, pokestopIds);
            var count = pokestops.LongCount(stop => HasPokestopQuestByType(stop, mode));
            return await Task.FromResult(Convert.ToUInt64(count));
        }

        public static async Task<List<Pokestop>> GetPokestopsInBoundsAsync(this MapDbContext context, IBoundingBox bbox, bool isEnabled = true)
        {
            var pokestops = context.Pokestops.Where(stop =>
                stop.Latitude >= bbox.MinimumLatitude &&
                stop.Longitude >= bbox.MinimumLongitude &&
                stop.Latitude <= bbox.MaximumLatitude &&
                stop.Longitude <= bbox.MaximumLongitude &&
                isEnabled == stop.IsEnabled &&
                !stop.IsDeleted
            ).ToList();
            return await Task.FromResult(pokestops);
        }

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

        #region S2 Cells

        /// <summary>
        /// Get all S2 cells
        /// </summary>
        /// <param name="context"></param>
        public static async Task<List<Cell>> GetS2CellsAsync(this MapDbContext context)
        {
            var cells = context.Cells.ToList();
            return await Task.FromResult(cells);
        }

        /// <summary>
        /// Get all S2 cells within bounding box
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bbox"></param>
        public static async Task<List<Cell>> GetS2CellsAsync(this MapDbContext context, IBoundingBox bbox)
        {
            var cells = context.Cells.Where(cell =>
                cell.Latitude >= bbox.MinimumLatitude &&
                cell.Longitude >= bbox.MinimumLongitude &&
                cell.Latitude <= bbox.MaximumLatitude &&
                cell.Longitude <= bbox.MaximumLongitude
            ).ToList();
            return await Task.FromResult(cells);
        }

        /// <summary>
        /// Get all S2 cells within geofence polygon
        /// </summary>
        /// <param name="context"></param>
        /// <param name="multiPolygon"></param>
        /// <returns></returns>
        public static async Task<List<Cell>> GetS2CellsAsync(this MapDbContext context, IMultiPolygon multiPolygon)
        {
            var bbox = multiPolygon.GetBoundingBox();
            // Get S2 cells within geofence bounding box
            var bboxCells = await GetS2CellsAsync(context, bbox);
            // Filter S2 cells outside of geofence polygon
            var cellsInArea = bboxCells.Where(cell => GeofenceService.InPolygon(multiPolygon, cell.ToCoordinate()))
                                       .ToList();
            return cellsInArea;
        }

        /// <summary>
        /// Get all S2 cells within geofence polygons
        /// </summary>
        /// <param name="context"></param>
        /// <param name="multiPolygons"></param>
        /// <returns></returns>
        public static async Task<List<Cell>> GetS2CellsAsync(this MapDbContext context, IEnumerable<IMultiPolygon> multiPolygons)
        {
            var cells = new List<Cell>();
            foreach (var multiPolygon in multiPolygons)
            {
                var list = await GetS2CellsAsync(context, multiPolygon);
                cells.AddRange(list);
            }
            return cells;
        }

        #endregion

        #region Private Methods

        private static bool HasPokestopQuestByType(Pokestop pokestop, QuestMode mode)
        {
            var result = mode == QuestMode.Normal
                ? pokestop.QuestType != null
                : mode == QuestMode.Alternative
                    ? pokestop.AlternativeQuestType != null
                    : pokestop.QuestType != null || pokestop.AlternativeQuestType != null;
            return result;
        }

        #endregion
    }
}