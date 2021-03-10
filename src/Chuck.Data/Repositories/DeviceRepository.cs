namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

    public class DeviceRepository : EfCoreRepository<Device, DeviceControllerContext>
    {
        public DeviceRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task ClearAllAccounts()
        {
            var devices = await GetAllAsync();
            foreach (var device in devices)
            {
                device.AccountUsername = null;
            }
            await InsertOrUpdate((List<Device>)devices).ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(Device device)
        {
            return await _dbContext.Devices
                .Upsert(device)
                .On(p => p.Uuid)
                .RunAsync()
                .ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<Device> devices)
        {
            return await _dbContext.Devices
                .UpsertRange(devices)
                .On(p => p.Uuid)
                .RunAsync()
                .ConfigureAwait(false);
        }
    }
}