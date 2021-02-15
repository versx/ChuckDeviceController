namespace ChuckDeviceController.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

    public class AccountRepository : EfCoreRepository<Account, DeviceControllerContext>
    {
        public AccountRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task<Account> GetNewAccountAsync(int minLevel, int maxLevel, List<string> inuseAccounts)
        {
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
            var accounts = await GetAllAsync().ConfigureAwait(false);
            var now = DateTime.UtcNow.ToTotalSeconds();
            const uint SpinLimit = 3500;
            const uint OneDaySeconds = 86400;
            const uint SevenDaySeconds = 7 * OneDaySeconds;
            // TODO: Use raw sql query or better alternative
            return new
            {
                new_count = (uint)accounts.Count(x => string.IsNullOrEmpty(x.Failed) &&
                                                 x.FailedTimestamp == null &&
                                                 x.FirstWarningTimestamp == null &&
                                                 x.LastEncounterLatitude == null &&
                                                 x.LastEncounterLongitude == null &&
                                                 x.LastEncounterTime == null &&
                                                 x.Spins == 0),
                in_use_count = (uint)accounts.Count(x => devices.Any(dev => string.Compare(dev.AccountUsername, x.Username, true) == 0)),
                clean_iv_count = (uint)accounts.Count(x => string.IsNullOrEmpty(x.Failed) &&
                                                      x.FailedTimestamp == null &&
                                                      x.FirstWarningTimestamp == null &&
                                                      x.LastEncounterLatitude == null &&
                                                      x.LastEncounterLongitude == null &&
                                                      x.LastEncounterTime == null &&
                                                      x.Spins == 0 &&
                                                      x.Level >= 30),
                total_iv_count = (uint)accounts.Count(x => x.Level >= 30),
                total = (uint)accounts.Count,
                failed_count = (uint)accounts.Count(x => !string.IsNullOrEmpty(x.Failed) || x.FailedTimestamp != null),
                in_cooldown_count = 0, // TODO: Cooldown
                over_spin_limit_count = (uint)accounts.Count(x => x.Spins >= SpinLimit),
                banned_1day = (uint)accounts.Count(x => x.FailedTimestamp > 0 && now - x.FailedTimestamp > OneDaySeconds),
                banned_7day = (uint)accounts.Count(x => x.FailedTimestamp > 0 && now - x.FailedTimestamp > SevenDaySeconds),
                banned_total = (uint)accounts.Count(x => x.FailedTimestamp > 0),
                warning_1day = (uint)accounts.Count(x => x.FirstWarningTimestamp > 0 && now - x.FailedTimestamp > OneDaySeconds),
                warning_7day = (uint)accounts.Count(x => x.FirstWarningTimestamp > 0 && now - x.FailedTimestamp > SevenDaySeconds),
                warning_total = accounts.Count(x => x.FirstWarningTimestamp > 0),
                all_account_stats = accounts.GroupBy(x => x.Level, (x, y) => new
                {
                    level = x,
                    total = accounts.Count(z => z.Level == x),
                    in_use = accounts.Count(z => devices.Any(dev => string.Compare(dev.AccountUsername, z.Username) == 0 && z.Level == x)),
                    good = y.Count(z => string.IsNullOrEmpty(z.Failed) && z.FailedTimestamp == null && z.FirstWarningTimestamp == null),
                    banned = y.Count(z => !string.IsNullOrEmpty(z.Failed) || z.FailedTimestamp > 0),
                    warning = y.Count(z => z.FirstWarningTimestamp > 0),
                    invalid = 0, // TODO: Invalid
                    other = 0, // TODO: Other
                    cooldown = 0, // TODO: Cooldown
                    spin_limit = y.Count(z => z.Spins >= SpinLimit),
                }).ToArray(),
            };
        }
    }
}