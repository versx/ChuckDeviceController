namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

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