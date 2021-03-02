namespace Chuck.Infrastructure.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class WebhookRepository : EfCoreRepository<Webhook, DeviceControllerContext>
    {
        public WebhookRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<IReadOnlyList<Webhook>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Webhooks.FromCache().ToList()).ConfigureAwait(false);
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }
    }
}