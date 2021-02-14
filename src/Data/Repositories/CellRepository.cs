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

    public class CellRepository : EfCoreRepository<Cell, DeviceControllerContext>
    {
        public CellRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
        }

        /*
        public async Task<IReadOnlyList<Cell>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Cells.FromCache().ToList());
            }
            return await base.GetAllAsync();
        }
        */

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
    }
}