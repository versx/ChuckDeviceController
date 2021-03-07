namespace Chuck.Infrastructure.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Extensions;

    public class WeatherRepository : EfCoreRepository<Weather, DeviceControllerContext>
    {
        public WeatherRepository(DeviceControllerContext context)
            : base(context)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<int> InsertOrUpdate(Weather weather)
        {
            return await _dbContext.Weather
                .Upsert(weather)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Weather
                {
                    CloudLevel = cDb.CloudLevel != cIns.CloudLevel
                        ? cIns.CloudLevel
                        : cDb.CloudLevel,
                    FogLevel = cDb.FogLevel != cIns.FogLevel
                        ? cIns.FogLevel
                        : cDb.FogLevel,
                    GameplayCondition = cDb.GameplayCondition != cIns.GameplayCondition
                        ? cIns.GameplayCondition
                        : cDb.GameplayCondition,
                    RainLevel = cDb.RainLevel != cIns.RainLevel
                        ? cIns.RainLevel
                        : cDb.RainLevel,
                    Severity = cDb.Severity != cIns.Severity
                        ? cIns.Severity
                        : cDb.Severity,
                    SnowLevel = cDb.SnowLevel != cIns.SnowLevel
                        ? cIns.SnowLevel
                        : cDb.SnowLevel,
                    SpecialEffectLevel = cDb.SpecialEffectLevel != cIns.SpecialEffectLevel
                        ? cIns.SpecialEffectLevel
                        : cDb.SpecialEffectLevel,
                    WarnWeather = cDb.WarnWeather != cIns.WarnWeather
                        ? cIns.WarnWeather
                        : cDb.WarnWeather,
                    WindDirection = cDb.WindDirection != cIns.WindDirection
                        ? cIns.WindDirection
                        : cDb.WindDirection,
                    WindLevel = cDb.WindLevel != cIns.WindLevel
                        ? cIns.WindLevel
                        : cDb.WindLevel,
                    Updated = DateTime.UtcNow.ToTotalSeconds(),
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<Weather> weather)
        {
            return await _dbContext.Weather
                .UpsertRange(weather)
                .On(p => p.Id)
                .WhenMatched((cDb, cIns) => new Weather
                {
                    CloudLevel = cDb.CloudLevel != cIns.CloudLevel
                        ? cIns.CloudLevel
                        : cDb.CloudLevel,
                    FogLevel = cDb.FogLevel != cIns.FogLevel
                        ? cIns.FogLevel
                        : cDb.FogLevel,
                    GameplayCondition = cDb.GameplayCondition != cIns.GameplayCondition
                        ? cIns.GameplayCondition
                        : cDb.GameplayCondition,
                    RainLevel = cDb.RainLevel != cIns.RainLevel
                        ? cIns.RainLevel
                        : cDb.RainLevel,
                    Severity = cDb.Severity != cIns.Severity
                        ? cIns.Severity
                        : cDb.Severity,
                    SnowLevel = cDb.SnowLevel != cIns.SnowLevel
                        ? cIns.SnowLevel
                        : cDb.SnowLevel,
                    SpecialEffectLevel = cDb.SpecialEffectLevel != cIns.SpecialEffectLevel
                        ? cIns.SpecialEffectLevel
                        : cDb.SpecialEffectLevel,
                    WarnWeather = cDb.WarnWeather != cIns.WarnWeather
                        ? cIns.WarnWeather
                        : cDb.WarnWeather,
                    WindDirection = cDb.WindDirection != cIns.WindDirection
                        ? cIns.WindDirection
                        : cDb.WindDirection,
                    WindLevel = cDb.WindLevel != cIns.WindLevel
                        ? cIns.WindLevel
                        : cDb.WindLevel,
                    Updated = DateTime.UtcNow.ToTotalSeconds(),
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Weather>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Weather.FromCache().ToList()).ConfigureAwait(false);
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }
    }
}