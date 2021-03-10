namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
    using Z.EntityFramework.Plus;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Extensions;

    public class AccountRepository : EfCoreRepository<Account, DeviceControllerContext>
    {
        public AccountRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task<int> InsertOrUpdate(Account account)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Accounts
                .Upsert(account)
                .On(p => p.Username)
                .WhenMatched((cDb, cIns) => new Account
                {
                    Level = cDb.Level != cIns.Level ? cIns.Level : cDb.Level,
                    Banned = cIns.Banned ?? cDb.Banned,
                    Warn = cIns.Warn ?? cDb.Warn,
                    SuspendedMessageAcknowledged = cIns.SuspendedMessageAcknowledged ?? cDb.SuspendedMessageAcknowledged,
                    WarnMessageAcknowledged = cIns.WarnMessageAcknowledged ?? cDb.WarnMessageAcknowledged,
                    CreationTimestamp = cIns.CreationTimestamp ?? cDb.CreationTimestamp,
                    FirstWarningTimestamp = cIns.FirstWarningTimestamp ?? cDb.FirstWarningTimestamp,
                    Failed = cIns.Failed ?? cDb.Failed,
                    FailedTimestamp = cIns.FailedTimestamp ?? cDb.FailedTimestamp,
                    WarnExpireTimestamp = cIns.WarnExpireTimestamp ?? cDb.WarnExpireTimestamp,
                    WasSuspended = cIns.WasSuspended ?? cDb.WasSuspended,
                    LastUsedTimestamp = cIns.LastUsedTimestamp ?? cDb.LastUsedTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<int> InsertOrUpdate(List<Account> accounts)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return await _dbContext.Accounts
                .UpsertRange(accounts)
                .On(p => p.Username)
                .WhenMatched((cDb, cIns) => new Account
                {
                    Level = cDb.Level != cIns.Level ? cIns.Level : cDb.Level,
                    Banned = cIns.Banned ?? cDb.Banned,
                    Warn = cIns.Warn ?? cDb.Warn,
                    SuspendedMessageAcknowledged = cIns.SuspendedMessageAcknowledged ?? cDb.SuspendedMessageAcknowledged,
                    WarnMessageAcknowledged = cIns.WarnMessageAcknowledged ?? cDb.WarnMessageAcknowledged,
                    CreationTimestamp = cIns.CreationTimestamp ?? cDb.CreationTimestamp,
                    FirstWarningTimestamp = cIns.FirstWarningTimestamp ?? cDb.FirstWarningTimestamp,
                    Failed = cIns.Failed ?? cDb.Failed,
                    FailedTimestamp = cIns.FailedTimestamp ?? cDb.FailedTimestamp,
                    WarnExpireTimestamp = cIns.WarnExpireTimestamp ?? cDb.WarnExpireTimestamp,
                    WasSuspended = cIns.WasSuspended ?? cDb.WasSuspended,
                    LastUsedTimestamp = cIns.LastUsedTimestamp ?? cDb.LastUsedTimestamp,
                })
                .RunAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Account>> GetAllAsync(bool fromCache = true)
        {
            if (fromCache)
            {
                return await Task.FromResult(_dbContext.Accounts.FromCache().ToList()).ConfigureAwait(false);
            }
            return await base.GetAllAsync().ConfigureAwait(false);
        }

        public async Task<Account> GetNewAccountAsync(int minLevel, int maxLevel, List<string> inuseAccounts)
        {
            var account = _dbContext.Accounts.DeferredCount().FromCacheAsync();
            var accounts = await GetAllAsync().ConfigureAwait(false);
            return accounts.FirstOrDefault(x =>
                x.Level >= minLevel &&
                x.Level <= maxLevel &&
                string.IsNullOrEmpty(x.Failed) &&
                x.Spins < 3500 &&
                x.FirstWarningTimestamp == null &&
                x.Warn == null &&
                x.WarnExpireTimestamp == null &&
                x.Banned == null &&
                !inuseAccounts.Contains(x.Username.ToLower())
            );
        }

        public async Task<bool> SetLastEncounterAsync(string username, double latitude, double longitude, ulong time)
        {
            var account = await GetByIdAsync(username).ConfigureAwait(false);
            if (account == null)
                return false;
            account.LastEncounterLatitude = latitude;
            account.LastEncounterLongitude = longitude;
            account.LastEncounterTime = time;
            await AddOrUpdateAsync(account).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> SpinAsync(string username)
        {
            var account = await GetByIdAsync(username).ConfigureAwait(false);
            if (account == null)
                return false;
            account.Spins++;
            await AddOrUpdateAsync(account).ConfigureAwait(false);
            return true;
        }

        public async Task<dynamic> GetStatsAsync()
        {
            var deviceRepository = new DeviceRepository(_dbContext);
            var devices = await deviceRepository.GetAllAsync().ConfigureAwait(false);
            var accounts = await GetAllAsync(true).ConfigureAwait(false);
            var now = DateTime.UtcNow.ToTotalSeconds();
            const uint SpinLimit = 3500;
            const uint OneDaySeconds = 86400;
            const uint SevenDaySeconds = 7 * OneDaySeconds;
            // TODO: Use raw sql query or better alternative
            return new
            {
                new_count = accounts.Count(x => string.IsNullOrEmpty(x.Failed) &&
                                                 x.FailedTimestamp == null &&
                                                 x.FirstWarningTimestamp == null &&
                                                 x.LastEncounterLatitude == null &&
                                                 x.LastEncounterLongitude == null &&
                                                 x.LastEncounterTime == null &&
                                                 x.Spins == 0).ToString("N0"),
                in_use_count = accounts.Count(x => devices.Any(dev => string.Compare(dev.AccountUsername, x.Username, true) == 0)).ToString("N0"),
                clean_iv_count = accounts.Count(x => string.IsNullOrEmpty(x.Failed) &&
                                                      x.FailedTimestamp == null &&
                                                      x.FirstWarningTimestamp == null &&
                                                      x.LastEncounterLatitude == null &&
                                                      x.LastEncounterLongitude == null &&
                                                      x.LastEncounterTime == null &&
                                                      x.Spins == 0 &&
                                                      x.Level >= 30).ToString("N0"),
                total_iv_count = accounts.Count(x => x.Level >= 30).ToString("N0"),
                total = accounts.Count.ToString("N0"),
                failed_count = accounts.Count(x => !string.IsNullOrEmpty(x.Failed) || x.FailedTimestamp != null).ToString("N0"),
                in_cooldown_count = 0, // TODO: Cooldown
                over_spin_limit_count = accounts.Count(x => x.Spins >= SpinLimit).ToString("N0"),
                banned_1day = accounts.Count(x => x.FailedTimestamp > 0 && now - x.FailedTimestamp > OneDaySeconds).ToString("N0"),
                banned_7day = accounts.Count(x => x.FailedTimestamp > 0 && now - x.FailedTimestamp > SevenDaySeconds).ToString("N0"),
                banned_total = accounts.Count(x => x.FailedTimestamp > 0).ToString("N0"),
                warning_1day = accounts.Count(x => x.FirstWarningTimestamp > 0 && now - x.FailedTimestamp > OneDaySeconds).ToString("N0"),
                warning_7day = accounts.Count(x => x.FirstWarningTimestamp > 0 && now - x.FailedTimestamp > SevenDaySeconds).ToString("N0"),
                warning_total = accounts.Count(x => x.FirstWarningTimestamp > 0).ToString("N0"),
                all_account_stats = accounts.GroupBy(x => x.Level, (x, y) => new
                {
                    level = x,
                    total = accounts.Count(z => z.Level == x).ToString("N0"),
                    in_use = accounts.Count(z => devices.Any(dev => string.Compare(dev.AccountUsername, z.Username) == 0 && z.Level == x)).ToString("N0"),
                    good = y.Count(z => string.IsNullOrEmpty(z.Failed) && z.FailedTimestamp == null && z.FirstWarningTimestamp == null).ToString("N0"),
                    banned = y.Count(z => !string.IsNullOrEmpty(z.Failed) || z.FailedTimestamp > 0).ToString("N0"),
                    warning = y.Count(z => z.FirstWarningTimestamp > 0).ToString("N0"),
                    invalid = 0, // TODO: Invalid
                    other = 0, // TODO: Other
                    cooldown = 0, // TODO: Cooldown
                    spin_limit = y.Count(z => z.Spins >= SpinLimit).ToString("N0"),
                }).ToArray(),
            };
        }

        public async Task<int> GetExpiredWarningsCount()
        {
            var expireTime = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)).ToTotalSeconds();
            var accounts = _dbContext.Accounts.Count(x => (x.FirstWarningTimestamp > 0 || x.Failed == "GPR_RED_WARNING") && x.FirstWarningTimestamp < expireTime);
            return await Task.FromResult(accounts).ConfigureAwait(false);
        }

        public async Task<int> GetExpiredBansCount()
        {
            var expireTime = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)).ToTotalSeconds();
            var accounts = _dbContext.Accounts.Count(x => (x.Failed == "banned" || x.Failed == "GPR_BANNED") && x.FailedTimestamp < expireTime);
            return await Task.FromResult(accounts).ConfigureAwait(false);
        }

        public async Task<int> ClearExpiredWarnings()
        {
            var expireTime = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)).ToTotalSeconds();
            var accounts = await _dbContext.Accounts.Where(x =>
                (x.FirstWarningTimestamp > 0 || x.WarnExpireTimestamp > 0 || x.Failed == "GPR_RED_WARNING") &&
                (x.FirstWarningTimestamp < expireTime || x.WarnExpireTimestamp < expireTime)
            ).ToListAsync().ConfigureAwait(false);
            accounts.ForEach(account =>
            {
                account.Warn = null;
                account.WarnExpireTimestamp = null;
                account.Failed = null;
                account.FirstWarningTimestamp = null;
            });
            return await InsertOrUpdate(accounts).ConfigureAwait(false);
        }

        public async Task<int> ClearExpiredBans()
        {
            var expireTime = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)).ToTotalSeconds();
            var accounts = await _dbContext.Accounts.Where(x =>
                (x.Failed == "banned" || x.Failed == "GPR_BANNED") && x.FailedTimestamp < expireTime
            ).ToListAsync().ConfigureAwait(false);
            accounts.ForEach(account =>
            {
                account.Banned = null;
                account.Failed = null;
                account.FailedTimestamp = null;
            });
            return await InsertOrUpdate(accounts).ConfigureAwait(false);
        }
    }
}