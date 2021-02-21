namespace ChuckDeviceController.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geofence.Models;

    public class CellRepository : EfCoreRepository<Cell, DeviceControllerContext>
    {
        public CellRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<IReadOnlyList<Cell>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Cells.FromCache().ToList()).ConfigureAwait(false);
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }

        public async Task<List<Cell>> GetAllAsync(BoundingBox bbox, ulong updated = 0)
        {
            var cells = await GetAllAsync(true).ConfigureAwait(false);
            return cells.Where(cell =>
                cell.Latitude >= bbox.MinimumLatitude &&
                cell.Latitude <= bbox.MaximumLatitude &&
                cell.Longitude >= bbox.MinimumLongitude &&
                cell.Longitude <= bbox.MaximumLongitude &&
                cell.Updated >= updated
            ).ToList();
        }

        public async Task<IReadOnlyList<Cell>> GetByIdsAsync(List<ulong> ids, bool fromCache = true)
        {
            return await Task.Run(() =>
                {
                    if (fromCache)
                    {
                        return _dbContext.Cells.Where(x => ids.Contains(x.Id))
                                               .FromCache()
                                               .ToList();
                    }
                    return _dbContext.Cells.Where(x => ids.Contains(x.Id))
                                           .ToList();
                }).ConfigureAwait(false);
        }

        public async Task<List<Cell>> GetByIdsAsync(List<ulong> ids)
        {
            const double MAX_COUNT = 10000.0;
            if (ids.Count > MAX_COUNT)
            {
                var list = new List<Cell>();
                var count = Math.Ceiling(ids.Count / MAX_COUNT);
                for (var i = 0; i < count; i++)
                {
                    var start = (int)MAX_COUNT * i;
                    var end = (int)Math.Min(MAX_COUNT * (i + 1), ids.Count - 1);
                    var slice = ids.Slice(start, end);
                    var result = await GetByIdsAsync(slice).ConfigureAwait(false);
                    if (result.Count > 0)
                    {
                        result.ForEach(x => list.Add(x));
                    }
                }
                return list;
            }
            if (ids.Count == 0)
            {
                return new List<Cell>();
            }
            return (List<Cell>)await GetByIdsAsync(ids, true).ConfigureAwait(false);
        }
    }
}