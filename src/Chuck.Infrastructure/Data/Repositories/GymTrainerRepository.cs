namespace Chuck.Infrastructure.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class GymTrainerRepository : EfCoreRepository<Trainer, DeviceControllerContext>
    {
        public GymTrainerRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task<int> InsertOrUpdate(Trainer trainer)
        {
            return await _dbContext.Trainers
                .Upsert(trainer)
                .On(p => p.Name)
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<Trainer> trainers)
        {
            return await _dbContext.Trainers
                .UpsertRange(trainers)
                .On(p => p.Name)
                .RunAsync().ConfigureAwait(false);
        }
    }
}