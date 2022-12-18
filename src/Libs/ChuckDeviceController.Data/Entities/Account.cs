namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using Microsoft.EntityFrameworkCore;
    using POGOProtos.Rpc;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contracts;
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
        private const string FailedBanned = "banned";
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
            JsonPropertyName("first_warning_timestamp"),
        ]
        public ulong? FirstWarningTimestamp { get; set; }

        [
            DisplayName("Failed Time"),
            Column("failed_timestamp"),
            JsonPropertyName("failed_timestamp"),
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
            JsonPropertyName("last_encounter_time"),
        ]
        public ulong? LastEncounterTime { get; set; }

        [
            DisplayName("Last Encounter Latitude"),
            Column("last_encounter_lat"),
            JsonPropertyName("last_encounter_lat"),
            Precision(18, 6),
        ]
        public double? LastEncounterLatitude { get; set; }

        [
            DisplayName("Last Encounter Longitude"),
            Column("last_encounter_lon"),
            JsonPropertyName("last_encounter_lon"),
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
            JsonPropertyName("creation_timestamp"),
        ]
        public ulong? CreationTimestamp { get; set; }

        [
            DisplayName("Has Warning"),
            Column("warn"),
            JsonPropertyName("warn"),
        ]
        public bool? HasWarn { get; set; }

        [
            DisplayName("Warning Expire Time"),
            Column("warn_expire_timestamp"),
            JsonPropertyName("warn_expire_timestamp"),
        ]
        public ulong? WarnExpireTimestamp { get; set; }

        [
            DisplayName("Warning Message Acknowledged"),
            Column("warn_message_acknowledged"),
            JsonPropertyName("warn_message_acknowledged"),
        ]
        public bool? WarnMessageAcknowledged { get; set; }

        [
            DisplayName("Suspended Message Acknowledged"),
            Column("suspended_message_acknowledged"),
            JsonPropertyName("suspended_message_acknowledged"),
        ]
        public bool? SuspendedMessageAcknowledged { get; set; }

        [
            DisplayName("Was Suspended"),
            Column("was_suspended"),
            JsonPropertyName("was_suspended"),
        ]
        public bool? WasSuspended { get; set; }

        [
            DisplayName("Banned"),
            Column("banned"),
            JsonPropertyName("banned"),
        ]
        public bool? IsBanned { get; set; }

        [
            DisplayName("Last Used"),
            Column("last_used_timestamp"),
            JsonPropertyName("last_used_timestamp"),
        ]
        public ulong? LastUsedTimestamp { get; set; } = 0;

        [
            DisplayName("Group"),
            Column("group"),
            JsonPropertyName("group"),
        ]
        public string? GroupName { get; set; }

        [
            DisplayName("Status"),
            NotMapped,
            JsonPropertyName("status"),
        ]
        public string Status => GetStatus();

        [
            DisplayName("Last Encounter"),
            NotMapped,
            JsonPropertyName("last_encounter"),
        ]
        public string LastEncounter => LastEncounterTime?
            .FromSeconds()
            .ToLocalTime()
            .ToString("hh:mm:ss tt MM/dd/yyyy") ?? "--";

        [
            DisplayName("In Use"),
            NotMapped,
            JsonPropertyName("in_use"),
        ]
        public bool IsInUse { get; set; }

        [
            NotMapped,
            JsonIgnore,
        ]
        public bool SendWebhook { get; set; }

        [
            NotMapped,
            JsonPropertyName("is_account_clean"),
        ]
        public bool IsAccountClean =>
            !IsAccountBanned &&
            !IsAccountSuspended &&
            !IsAccountWarned &&
            !IsAccountInvalidCredentials &&
            !IsAccountInCooldown &&
            Spins < 3500;

        [
            NotMapped,
            JsonPropertyName("is_account_banned"),
        ]
        public bool IsAccountBanned => Failed == FailedBanned || Failed == FailedGprBanned || (IsBanned ?? false);

        [
            NotMapped,
            JsonPropertyName("is_account_warned"),
        ]
        public bool IsAccountWarned =>
            (Failed == FailedGprRedWarning && FailedTimestamp >= DateTime.UtcNow.ToTotalSeconds() - WarningPeriodS) ||
            (FirstWarningTimestamp > 0 && FirstWarningTimestamp >= DateTime.UtcNow.ToTotalSeconds() - WarningPeriodS) ||
            (HasWarn ?? false && WarnExpireTimestamp >= DateTime.UtcNow.ToTotalSeconds() - WarningPeriodS);

        [
            NotMapped,
            JsonPropertyName("is_account_suspended"),
        ]
        public bool IsAccountSuspended => 
            (Failed == FailedSuspended || (WasSuspended ?? false)) &&
            FailedTimestamp >= DateTime.UtcNow.ToTotalSeconds() - SuspendedPeriodS;

        [
            NotMapped,
            JsonPropertyName("is_account_invalid_credentials"),
        ]
        public bool IsAccountInvalidCredentials => Failed == FailedInvalidCredentials;

        [
            NotMapped,
            JsonPropertyName("is_account_in_cooldown"),
        ]
        public bool IsAccountInCooldown => LastEncounterTime > 0 && LastEncounterTime >= DateTime.UtcNow.ToTotalSeconds() - CooldownPeriodS;

        [
            NotMapped,
            JsonPropertyName("is_level_40_or_higher"),
        ]
        public bool IsLevel40OrHigher => Level >= 40;

        [
            NotMapped,
            JsonPropertyName("is_level_30_or_higher"),
        ]
        public bool IsLevel30OrHigher => Level >= 30 && Level < 40;

        [
            NotMapped,
            JsonPropertyName("is_new"),
        ]
        public bool IsNewAccount => Level == 0;

        #endregion

        #region Public Methods

        public async Task UpdateAsync(GetPlayerOutProto playerData, IMemoryCacheHostedService memCache)
        {
            CreationTimestamp = Convert.ToUInt32(playerData.Player.CreationTimeMs / 1000);
            if (playerData.Warn != HasWarn)
            {
                HasWarn = playerData.Warn;
                SendWebhook = true;
            }
            var warnExpireTimestamp = Convert.ToUInt32(playerData.WarnExpireMs / 1000);
            if (WarnExpireTimestamp != warnExpireTimestamp)
            {
                WarnExpireTimestamp = warnExpireTimestamp;
                SendWebhook = true;
            }
            if (WarnMessageAcknowledged != playerData.WarnMessageAcknowledged)
            {
                WarnMessageAcknowledged = playerData.WarnMessageAcknowledged;
                SendWebhook = true;
            }
            if (SuspendedMessageAcknowledged != playerData.SuspendedMessageAcknowledged)
            {
                SuspendedMessageAcknowledged = playerData.SuspendedMessageAcknowledged;
                SendWebhook = true;
            }
            if (WasSuspended != playerData.WasSuspended)
            {
                WasSuspended = playerData.WasSuspended;
                SendWebhook = true;
            }
            if (IsBanned != playerData.Banned)
            {
                IsBanned = playerData.Banned;
                SendWebhook = true;
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            if ((playerData.Warn || playerData.WarnMessageAcknowledged) && string.IsNullOrEmpty(Failed))
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
                SendWebhook = true;
                Console.WriteLine($"[{Username}] Account '{playerData.Player.Name}' (Username: {Username}) Has Red Warning");
            }

            if ((playerData.WasSuspended || playerData.SuspendedMessageAcknowledged) &&
                (string.IsNullOrEmpty(Failed) || Failed == FailedGprRedWarning))
            {
                // Occurs if an account was suspended and backend was not aware. Caused
                // by manual database manipulation or similar.
                Failed = FailedSuspended;
                FailedTimestamp = now - SuspendedPeriodS;
                SendWebhook = true;
                Console.WriteLine($"[{Username}] Account '{playerData.Player.Name}' (Username: {Username}) Was Suspended");
            }

            if (playerData.Banned)
            {
                Failed = FailedGprBanned;
                FailedTimestamp = now;
                SendWebhook = true;
                Console.WriteLine($"[{Username}] Account '{playerData.Player.Name}' (Username: {Username}) Banned");
            }

            LastUsedTimestamp ??= now;

            // Cache account entity by username
            memCache.Set(Username, this);

            await Task.CompletedTask;
        }

        public bool IsValid(ushort minLevel, ushort maxLevel, bool ignoreWarning = false, string? groupName = null)
        {
            var matchesGroup = string.Compare(GroupName, groupName, true) == 0;
            var matchesLevel = Level >= minLevel && Level <= maxLevel;
            var matches = matchesGroup && matchesLevel;
            var isValid = matches && (IsAccountClean || (IsAccountWarned && ignoreWarning));
            return isValid;
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