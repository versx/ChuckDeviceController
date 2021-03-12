namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Z.EntityFramework.Plus;

    using Chuck.Data.Entities;
    using Chuck.Data.Interfaces;
    using Chuck.Extensions;

    /// <summary>
    /// "There's some repetition here - couldn't we have some the sync methods call the async?"
    /// https://blogs.msdn.microsoft.com/pfxteam/2012/04/13/should-i-expose-synchronous-wrappers-for-asynchronous-methods/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EfCoreRepository<TEntity, TContext>
        where TEntity : BaseEntity, IAggregateRoot
        where TContext : DbContext
    {
        protected readonly TContext _dbContext;

        public EfCoreRepository(TContext dbContext)
        {
            QueryCacheManager.Cache = new MemoryCache(new MemoryCacheOptions());
            _dbContext = dbContext;
        }

        #region GetById

        public virtual async Task<TEntity> GetByIdAsync(uint id)
        {
            var keyValues = new object[] { id };
            return await _dbContext.Set<TEntity>().FindAsync(keyValues).ConfigureAwait(false);
        }

        public virtual async Task<TEntity> GetByIdAsync(long id)
        {
            var keyValues = new object[] { id };
            return await _dbContext.Set<TEntity>().FindAsync(keyValues).ConfigureAwait(false);
        }

        public virtual async Task<TEntity> GetByIdAsync(ulong id)
        {
            var keyValues = new object[] { id };
            return await _dbContext.Set<TEntity>().FindAsync(keyValues).ConfigureAwait(false);
        }

        public virtual async Task<TEntity> GetByIdAsync(string id)
        {
            var keyValues = new object[] { id };
            return await _dbContext.Set<TEntity>().FindAsync(keyValues).ConfigureAwait(false);
        }

        public virtual async Task<TEntity> GetByAsync(ISpecification<TEntity> spec)
        {
            return await _dbContext.Set<TEntity>()
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(spec.Criteria)
                                   .ConfigureAwait(false);
        }

        #endregion

        #region List

        public virtual async Task<List<TEntity>> GetByIdsAsync(List<string> ids)
        {
            var list = new List<TEntity>();
            foreach (var id in ids)
            {
                var item = await GetByIdAsync(id).ConfigureAwait(false);
                list.Add(item);
            }
            return list;
        }

        public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync()
        {
            return await _dbContext.Set<TEntity>()
                                   .AsNoTracking()
                                   .ToListAsync()
                                   .ConfigureAwait(false);
        }

        public virtual async Task<List<TEntity>> GetAllAsync(ISpecification<TEntity> spec)
        {
            return await _dbContext.Set<TEntity>()
                                   .AsNoTracking()
                                   .Where(spec.Criteria)
                                   .ToListAsync()
                                   .ConfigureAwait(false);
        }

        #endregion

        public virtual async Task<bool> ContainsAsync(TEntity entity)
        {
            return await _dbContext.Set<TEntity>().ContainsAsync(entity).ConfigureAwait(false);
        }

        public virtual bool ExistsAsync(TEntity entity)
        {
            return _dbContext.Entry(entity).IsKeySet;
        }

        /*
        public virtual Task InsertOrUpdate(TEntity entity)
        {
            _dbContext.Entry(entity).Upsert(entity)
                .On(p => p)
                .WhenMatched(c => new TEntity
                {
                });
            //
            var existings = await _dbContext.FindAsync(typeof(TEntity), entity);
            if (existings == null)
            {
                _dbContext.Add(entity);
            }
            else
            {
                _dbContext.Entry(existings).CurrentValues.SetValues(entity);
            }
            //
            _dbContext.Update(entity);
            return _dbContext.SaveChangesAsync();
        }
        */

        /*
        public virtual Task InsertOrUpdate(List<TEntity> entities)
        {
            //entities.ForEach(async entity => await InsertOrUpdate(entity));
            _dbContext.UpdateRange(entities);
            return _dbContext.SaveChangesAsync();
        }
        */

        #region Add

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return entity;
        }

        public virtual async Task AddRangeAsync(List<TEntity> entities)
        {
            await _dbContext.AddRangeAsync(entities).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        #endregion

        #region Update

        public virtual async Task UpdateAsync(TEntity entity)
        {
            _dbContext.Update(entity);
            _dbContext.Entry(entity).State = EntityState.Modified;//.Detached;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public virtual async Task UpdateRangeAsync(List<TEntity> entities)
        {
            _dbContext.UpdateRange(entities);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        #endregion

        public virtual async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        #region Delete

        public virtual async Task DeleteAsync(TEntity entity)
        {
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public virtual async Task DeleteAllAsync()
        {
            _dbContext.RemoveRange(_dbContext.Set<TEntity>());
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public virtual async Task DeleteRangeAsync(List<TEntity> entities)
        {
            _dbContext.RemoveRange(entities);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /*
        public virtual async Task DeleteByIdAsync(string key)
        {
            var entity = _dbContext.Set<TEntity>().FirstOrDefault(x => x.Id == key);
            await _dbContext.Set<TEntity>().DeleteAsync(entity);
        }
        */

        #endregion

        public virtual async Task AddOrUpdateAsync(TEntity entity)
        {
            try
            {
                _dbContext.SingleMerge(entity);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (MySqlConnector.MySqlException ex)
            {
                ConsoleExt.WriteError($"AddOrUpdateAsync: {ex}");
            }
        }

        public virtual async Task AddOrUpdateAsync(List<TEntity> entities)
        {
            try
            {
                await _dbContext.BulkMergeAsync(entities, x =>
                {
                    x.AutoMap = Z.BulkOperations.AutoMapType.ByIndexerName;
                    x.BatchSize = 100;
                    //x.BatchTimeout = 10 * 1000;
                    x.InsertIfNotExists = true;
                    x.InsertKeepIdentity = true;
                    x.MergeKeepIdentity = true;
                    x.Resolution = Z.BulkOperations.ResolutionType.Smart;
                    x.UseTableLock = true;
                    x.AllowDuplicateKeys = true;
                    //x.ColumnPrimaryKeyExpression = entity => entity.Id || entity.Uuid;
                }).ConfigureAwait(false);
            }
            catch (MySqlConnector.MySqlException ex)
            {
                ConsoleExt.WriteError($"[EfCoreRepository] AddOrUpdateAsync: {ex.Message}");
            }
        }
    }
}