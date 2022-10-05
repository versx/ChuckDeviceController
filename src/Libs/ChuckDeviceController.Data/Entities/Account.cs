namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;

    [Table("account")]
    public class Account : BaseEntity, IAccount, IWebhookEntity
    {
        #region Constants

        private const uint SuspendedPeriod = 2592000;
        private const uint WarningPeriod = 604800;
        private const string FailedGprBanned = "GPR_BANNED";
        private const string FailedGprRedWarning = "GPR_RED_WARNING";
        private const string FailedSuspended = "suspended";

        #endregion

        #region Properties

        [
            DisplayName("Username"),
            Column("username"),
            Key,
        ]
        public string Username { get; set; }

        [
            DisplayName("Password"),
            Column("password"),
            Required,
        ]
        public string Password { get; set; }

        [
            DisplayName("First Warning Time"),
            Column("first_warning_timestamp"),
        ]
        public ulong? FirstWarningTimestamp { get; set; }

        [
            DisplayName("Failed Time"),
            Column("failed_timestamp"),
        ]
        public ulong? FailedTimestamp { get; set; }

        [
            DisplayName("Failed"),
            Column("failed"),
        ]
        public string? Failed { get; set; }

        [
            DisplayName("Level"),
            Column("level"),
        ]
        public ushort Level { get; set; }

        [
            DisplayName("Last Encounter Time"),
            Column("last_encounter_time"),
        ]
        public ulong? LastEncounterTime { get; set; }

        [
            DisplayName("Last Encounter Latitude"),
            Column("last_encounter_lat"),
        ]
        public double? LastEncounterLatitude { get; set; }

        [
            DisplayName("Last Encounter Longitude"),
            Column("last_encounter_lon"),
        ]
        public double? LastEncounterLongitude { get; set; }

        [
            DisplayName("Spins"),
            Column("spins"),
        ]
        public uint Spins { get; set; } = 0;

        [
            DisplayName("Tutorial"),
            Column("tutorial"),
        ]
        public ushort Tutorial { get; set; } = 0;

        [
            DisplayName("Created"),
            Column("creation_timestamp"),
        ]
        public ulong? CreationTimestamp { get; set; }

        [
            DisplayName("Has Warning"),
            Column("warn"),
        ]
        public bool? HasWarn { get; set; }

        [
            DisplayName("Warning Expire Time"),
            Column("warn_expire_timestamp"),
        ]
        public ulong? WarnExpireTimestamp { get; set; }

        [
            DisplayName("Warning Message Acknowledged"),
            Column("warn_message_acknowledged"),
        ]
        public bool? WarnMessageAcknowledged { get; set; }

        [
            DisplayName("Suspended Message Acknowledged"),
            Column("suspended_message_acknowledged"),
        ]
        public bool? SuspendedMessageAcknowledged { get; set; }

        [
            DisplayName("Was Suspended"),
            Column("was_suspended"),
        ]
        public bool? WasSuspended { get; set; }

        [
            DisplayName("Banned"),
            Column("banned"),
        ]
        public bool? IsBanned { get; set; }

        [
            DisplayName("Last Used"),
            Column("last_used_timestamp"),
        ]
        public ulong? LastUsedTimestamp { get; set; } = 0;

        [
            DisplayName("Group"),
            Column("group"),
        ]
        public string? GroupName { get; set; }

        [
            DisplayName("Status"),
            NotMapped,
        ]
        public string Status => GetStatus();

        [
            DisplayName("Last Encounter"),
            NotMapped,
        ]
        public string LastEncounter => LastEncounterTime?
            .FromSeconds()
            .ToLocalTime()
            .ToString("hh:mm:ss tt MM/dd/yyyy") ?? "--";

        [
            DisplayName("In Use"),
            NotMapped,
        ]
        public bool IsInUse { get; set; }

        [NotMapped]
        public bool SendWebhook { get; set; }

        #endregion

        #region Public Methods

        public async Task UpdateAsync(ControllerDbContext context, GetPlayerOutProto accountData, IMemoryCacheHostedService memCache)
        {
            CreationTimestamp = Convert.ToUInt32(accountData.Player.CreationTimeMs / 1000);
            HasWarn = accountData.Warn;
            var warnExpireTimestamp = Convert.ToUInt32(accountData.WarnExpireMs / 1000);
            if (warnExpireTimestamp > 0)
            {
                WarnExpireTimestamp = warnExpireTimestamp;
            }
            WarnMessageAcknowledged = accountData.WarnMessageAcknowledged;
            SuspendedMessageAcknowledged = accountData.SuspendedMessageAcknowledged;
            WasSuspended = accountData.WasSuspended;
            IsBanned = accountData.Banned;

            var now = DateTime.UtcNow.ToTotalSeconds();

            if ((accountData.Warn || accountData.WarnMessageAcknowledged) && string.IsNullOrEmpty(Failed))
            {
                Failed = FailedGprRedWarning;
                if (warnExpireTimestamp > now)
                {
                    if (FirstWarningTimestamp == null)
                    {
                        FirstWarningTimestamp = now;
                    }
                    FailedTimestamp = now;
                }
                else
                {
                    if (FirstWarningTimestamp == null)
                    {
                        FirstWarningTimestamp = warnExpireTimestamp > 0
                            ? warnExpireTimestamp - WarningPeriod
                            : now - WarningPeriod;
                    }
                    FailedTimestamp = now - WarningPeriod;
                }
                Console.WriteLine($"[{Username}] Account '{accountData.Player.Name}' (Username: {Username}) Has Red Warning");
            }

            if ((accountData.WasSuspended || accountData.SuspendedMessageAcknowledged) &&
                    (string.IsNullOrEmpty(Failed) || Failed == FailedGprRedWarning))
            {
                // Occurs if an account was suspended and backend was not aware. Caused
                // by manual database manipulation or similar.
                Failed = FailedSuspended;
                FailedTimestamp = now - SuspendedPeriod;
                Console.WriteLine($"[{Username}] Account '{accountData.Player.Name}' (Username: {Username}) Was Suspended");
            }

            if (accountData.Banned)
            {
                Failed = FailedGprBanned;
                FailedTimestamp = now;
                Console.WriteLine($"[{Username}] Account '{accountData.Player.Name}' (Username: {Username}) Banned");
            }

            Account? oldAccount = null;
            try
            {
                // Check cache first for account entity
                var cached = memCache.Get<string, Account>(Username);
                if (cached != null)
                {
                    oldAccount = cached;
                }
                else
                {
                    oldAccount = await context.Accounts.FindAsync(Username);
                    if (oldAccount != null)
                    {
                        memCache.Set(Username, oldAccount);
                    }
                    else
                    {
                        memCache.Set(Username, this);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account: {ex}");
            }

            if (oldAccount == null)
            {
                SendWebhook = true;
            }
            else
            {
                if (LastEncounterLatitude == null && oldAccount.LastEncounterLatitude != null)
                {
                    LastEncounterLatitude = oldAccount.LastEncounterLatitude;
                }
                if (LastEncounterLongitude == null && oldAccount.LastEncounterLongitude != null)
                {
                    LastEncounterLongitude = oldAccount.LastEncounterLongitude;
                }
                if (LastEncounterTime == null && oldAccount.LastEncounterTime != null)
                {
                    LastEncounterTime = oldAccount.LastEncounterTime;
                }
                if (string.IsNullOrEmpty(Failed) && !string.IsNullOrEmpty(oldAccount.Failed))
                {
                    Failed = oldAccount.Failed;
                }
                if (FirstWarningTimestamp == null && oldAccount.FirstWarningTimestamp != null)
                {
                    FirstWarningTimestamp = oldAccount.FirstWarningTimestamp;
                }
                if (FailedTimestamp == null && oldAccount.FailedTimestamp != null)
                {
                    FailedTimestamp = oldAccount.FailedTimestamp;
                }
                if (Spins < oldAccount.Spins)
                {
                    Spins = oldAccount.Spins;
                }
                if (CreationTimestamp == null && oldAccount.CreationTimestamp != null)
                {
                    CreationTimestamp = oldAccount.CreationTimestamp;
                }
                if (HasWarn == null && oldAccount.HasWarn != null)
                {
                    HasWarn = oldAccount.HasWarn;
                }
                if (WarnExpireTimestamp == null && oldAccount.WarnExpireTimestamp != null)
                {
                    WarnExpireTimestamp = oldAccount.WarnExpireTimestamp;
                }
                if (WarnMessageAcknowledged == null && oldAccount.WarnMessageAcknowledged != null)
                {
                    WarnMessageAcknowledged = oldAccount.WarnMessageAcknowledged;
                }
                if (SuspendedMessageAcknowledged == null && oldAccount.SuspendedMessageAcknowledged != null)
                {
                    SuspendedMessageAcknowledged = oldAccount.SuspendedMessageAcknowledged;
                }
                if (WasSuspended == null && oldAccount.WasSuspended != null)
                {
                    WasSuspended = oldAccount.WasSuspended;
                }
                if (IsBanned == null && oldAccount.IsBanned != null)
                {
                    IsBanned = oldAccount.IsBanned;
                }
                if (LastUsedTimestamp == null && oldAccount.LastUsedTimestamp != null)
                {
                    LastUsedTimestamp = oldAccount.LastUsedTimestamp;
                }

                SendWebhook = Level != oldAccount.Level ||
                    Failed != oldAccount.Failed ||
                    HasWarn != oldAccount.HasWarn ||
                    IsBanned != oldAccount.IsBanned;
            }

            // Cache account entity by username
            memCache.Set(Username, this);

            await Task.CompletedTask;
        }

        public bool IsValid(bool ignoreWarning = false, string? groupName = null)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return (
                    string.Compare(GroupName, groupName, true) == 0 &&
                    string.IsNullOrEmpty(Failed)
                ) || (
                    Failed == FailedGprRedWarning &&
                    (ignoreWarning || (WarnExpireTimestamp ?? ulong.MaxValue) <= now)
                ) || (
                    Failed == FailedSuspended &&
                    (FailedTimestamp ?? ulong.MaxValue) <= now - 2592000
                );
        }

        public string GetStatus()
        {
            // TODO: Check against ban/warn times
            if (string.Compare(Failed, "banned", true) == 0)
                return "Banned";
            if (string.Compare(Failed, "GPR_BANNED", true) == 0)
                return "Banned";
            if (IsBanned ?? false)
                return "Banned";
            if (FirstWarningTimestamp > 0)
                return "Warning";
            if (string.Compare(Failed, "GPR_RED_WARNING", true) == 0)
                return "Warning";
            if (HasWarn ?? false)
                return "Warning";
            if (WasSuspended ?? false)
                return "Warning";
            if (string.Compare(Failed, "suspended", true) == 0)
                return "Suspended";
            if (string.Compare(Failed, "invalid_credentials", true) == 0)
                return "Invalid";
            // TODO: Cooldown?
            // TODO: InUse?
            return "Good";
        }

        public dynamic? GetWebhookData(string type)
        {
            switch (type.ToLower())
            {
                case "account":
                    return new
                    {
                        type = WebhookHeaders.Account,
                        message = new
                        {
                            username = Username,
                            level = Level,
                            first_warning_timestamp = FirstWarningTimestamp ?? 0,
                            failed_timestamp = FailedTimestamp ?? 0,
                            failed = Failed ?? "None",
                            last_encounter_time = LastEncounterTime ?? 0,
                            spins = Spins,
                            creation_timestamp = CreationTimestamp,
                            warn = HasWarn ?? false,
                            warn_expire_timestamp = WarnExpireTimestamp ?? 0,
                            warn_message_acknowledged = WarnMessageAcknowledged ?? false,
                            suspended_message_acknowledged = SuspendedMessageAcknowledged ?? false, 
                            was_suspended = WasSuspended ?? false,
                            banned = IsBanned ?? false,
                            group = GroupName,
                        },
                    };
            }

            Console.WriteLine($"Received unknown account webhook payload type: {type}, returning null");
            return null;
        }

        #endregion
    }
}