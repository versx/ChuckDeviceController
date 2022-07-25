namespace ChuckDeviceController.Data.Extensions
{
    using System;
    using System.Collections.Generic;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    public static class MapDataContextExtensions
    {
        #region Clear Quests

        /// <summary>
        /// Clear all Pokestops with quests
        /// </summary>
        /// <param name="context"></param>
        public static async Task ClearQuestsAsync(this MapDataContext context)
        {
            var pokestopIds = context.Pokestops.Select(stop => stop.Id)
                                               .ToList();
            await ClearQuestsAsync(context, pokestopIds);
        }

        /// <summary>
        /// Clear all Pokestops with quests by Pokestop IDs
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pokestopIds"></param>
        public static async Task ClearQuestsAsync(this MapDataContext context, IEnumerable<string> pokestopIds)
        {
            var pokestopsToUpsert = new List<Pokestop>();
            var pokestops = context.Pokestops
                                   .Where(pokestop => pokestopIds.Contains(pokestop.Id))
                                   .ToList();
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

                pokestopsToUpsert.Add(pokestop);
            });
            await context.Pokestops.BulkUpdateAsync(pokestopsToUpsert, options =>
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
        /// Clear all Pokestop quests within geofences
        /// </summary>
        /// <param name="context"></param>
        /// <param name="multiPolygons"></param>
        public static async Task ClearQuestsAsync(this MapDataContext context, List<MultiPolygon> multiPolygons)
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
        public static async Task ClearQuestsAsync(this MapDataContext context, MultiPolygon multiPolygon)
        {
            var bbox = multiPolygon.GetBoundingBox();
            await ClearQuestsAsync(context, bbox);
        }

        /// <summary>
        /// Clear all Pokestop quests within bounding box
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bbox"></param>
        public static async Task ClearQuestsAsync(this MapDataContext context, BoundingBox bbox)
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

        #endregion
    }
}