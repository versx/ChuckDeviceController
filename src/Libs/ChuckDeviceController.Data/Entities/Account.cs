namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Microsoft.EntityFrameworkCore;
    using MySqlConnector;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;

    [Table("account")]
    public class Account : BaseEntity, IAccount, IWebhookEntity
    {
        #region Constants

        private const uint SuspendedPeriodS = 2592000;
        private const uint WarningPeriodS = 604800;
        private const uint CooldownPeriodS = 7200;
        private const string FailedGprBanned = "GPR_BANNED";
        private const string FailedGprRedWarning = "GPR_RED_WARNING";
        private const string FailedSuspended = "suspended";
        private const string FailedInvalidCredentials = "invalid_credentials";

        public static readonly IEnumerable<string> FailedReasons = new List<string>
        {
            "banned",
            "suspended",
            "invalid_credentials",
            "GPR_RED_WARNING",
            "GPR_BANNED",
        };

        #endregion

        #region Properties

        [
            DisplayName("Username"),
            Column("username"),
            Key,
        ]
        public string Username { get; set; } = null!;

        [
            DisplayName("Password"),
            Column("password"),
            Required,
        ]
        public string Password { get; set; } = null!;

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
            Precision(18, 6),
        ]
        public double? LastEncounterLatitude { get; set; }

        [
            DisplayName("Last Encounter Longitude"),
            Column("last_encounter_lon"),
            Precision(18, 6),
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

        [NotMapped]
        public bool IsAccountClean => string.IsNullOrEmpty(Failed) && !IsAccountInCooldown && Spins < 3500;

        [NotMapped]
        public bool IsAccountBanned => Failed == "banned" || Failed == FailedGprBanned || (IsBanned ?? false);

        [NotMapped]
        public bool IsAccountWarned =>
            (Failed == FailedGprRedWarning && FailedTimestamp >= DateTime.UtcNow.ToTotalSeconds() - WarningPeriodS) ||
            (FirstWarningTimestamp > 0 && FirstWarningTimestamp >= DateTime.UtcNow.ToTotalSeconds() - WarningPeriodS) ||
            (HasWarn ?? false && WarnExpireTimestamp >= DateTime.UtcNow.ToTotalSeconds() - WarningPeriodS);

        [NotMapped]
        public bool IsAccountSuspended => 
            (Failed == FailedSuspended || (WasSuspended ?? false)) &&
            FailedTimestamp >= DateTime.UtcNow.ToTotalSeconds() - SuspendedPeriodS;

        [NotMapped]
        public bool IsAccountInvalidCredentials => Failed == FailedInvalidCredentials;

        [NotMapped]
        public bool IsAccountInCooldown => LastEncounterTime > 0 && CooldownPeriodS >= DateTime.UtcNow.ToTotalSeconds() - LastEncounterTime;

        [NotMapped]
        public bool IsLevel40OrHigher => Level >= 40;

        [NotMapped]
        public bool IsLevel30OrHigher => Level >= 30 && Level < 40;

        [NotMapped]
        public bool IsNewAccount => Level == 0;

        #endregion

        #region Public Methods

        public async Task UpdateAsync(MySqlConnection connection, GetPlayerOutProto accountData, IMemoryCacheHostedService memCache, bool skipLookup = false)
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
                    FirstWarningTimestamp ??= now;
                    FailedTimestamp = now;
                }
                else
                {
                    FirstWarningTimestamp ??= warnExpireTimestamp > 0
                        ? warnExpireTimestamp - WarningPeriodS
                        : now - WarningPeriodS;
                    FailedTimestamp = now - WarningPeriodS;
                }
                Console.WriteLine($"[{Username}] Account '{accountData.Player.Name}' (Username: {Username}) Has Red Warning");
            }

            if ((accountData.WasSuspended || accountData.SuspendedMessageAcknowledged) &&
                    (string.IsNullOrEmpty(Failed) || Failed == FailedGprRedWarning))
            {
                // Occurs if an account was suspended and backend was not aware. Caused
                // by manual database manipulation or similar.
                Failed = FailedSuspended;
                FailedTimestamp = now - SuspendedPeriodS;
                Console.WriteLine($"[{Username}] Account '{accountData.Player.Name}' (Username: {Username}) Was Suspended");
            }

            if (accountData.Banned)
            {
                Failed = FailedGprBanned;
                FailedTimestamp = now;
                Console.WriteLine($"[{Username}] Account '{accountData.Player.Name}' (Username: {Username}) Banned");
            }

            var oldAccount = skipLookup
                ? null
                : await EntityRepository.GetEntityAsync<string, Account>(connection, Username, memCache);
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
            if (IsAccountBanned)
                return "Banned";
            if (IsAccountWarned)
                return "Warning";
            if (IsAccountSuspended)
                return "Suspended";
            if (IsAccountInvalidCredentials)
                return "Invalid";
            if (IsAccountInCooldown)
                return "Cooldown";

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