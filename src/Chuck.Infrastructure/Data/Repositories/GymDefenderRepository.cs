namespace Chuck.Infrastructure.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class GymDefenderRepository : EfCoreRepository<GymDefender, DeviceControllerContext>
    {
        public GymDefenderRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task<int> InsertOrUpdate(GymDefender defender)
        {
            return await _dbContext.GymDefenders
                .Upsert(defender)
                .On(p => p.Id)
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<GymDefender> defenders)
        {
            return await _dbContext.GymDefenders
                .UpsertRange(defenders)
                .On(p => p.Id)
                .RunAsync().ConfigureAwait(false);
        }
    }
}