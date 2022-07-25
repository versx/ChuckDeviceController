namespace ChuckDeviceController.Data.Extensions
{
    using System;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

    public static class DeviceControllerContextExtensions
    {
        private const ushort CooldownLimitS = 7200; // 2 hours
        private const uint SuspensionTimeLimitS = 2592000; // 30 days - Max account suspension time period

        #region Accounts

        public static async Task<Account?> GetAccountAsync(this DeviceControllerContext context, string username)
        {
            var account = await context.Accounts.FindAsync(username);
            return account;
        }

        public static async Task<Account?> GetNewAccountAsync(this DeviceControllerContext context,
            ushort minLevel = 0, ushort maxLevel = 35, bool ignoreWarning = false, uint spins = 3500,
            bool noCooldown = true, string? group = null,
            ushort cooldownLimitS = CooldownLimitS, uint suspensionTimeLimitS = SuspensionTimeLimitS)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var account = context.Accounts.FirstOrDefault(a =>
                // Meet level requirements for instance
                a.Level >= minLevel && a.Level <= maxLevel &&
                // Is under total spins
                a.Spins < spins &&
                // Matches event group name
                !string.IsNullOrEmpty(group)
                    ? a.GroupName == group
                    : string.IsNullOrEmpty(a.GroupName) &&
                // 
                noCooldown
                    ? (a.LastEncounterTime == null || now - a.LastEncounterTime >= cooldownLimitS)
                    : (a.LastEncounterTime == null || a.LastEncounterTime != null) &&
                ignoreWarning
                    // Has warning 
                    ? string.IsNullOrEmpty(a.Failed) || a.Failed == "GPR_RED_WARNING"
                    // Has no account warnings or are expired already
                    : (a.Failed == null && a.FirstWarningTimestamp == null) ||
                      (a.Failed == "GPR_RED_WARNING" && a.WarnExpireTimestamp > 0 && a.WarnExpireTimestamp <= now) ||
                      (a.Failed == "suspended" && a.FailedTimestamp <= now - suspensionTimeLimitS)
            );
            return await Task.FromResult(account);
        }

        #endregion
    }
}