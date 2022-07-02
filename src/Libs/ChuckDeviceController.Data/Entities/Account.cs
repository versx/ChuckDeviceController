namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using ChuckDeviceController.Extensions;

    [Table("account")]
    public class Account : BaseEntity
    {
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
            DisplayName("First Warning Timestamp"),
            Column("first_warning_timestamp"),
        ]
        public ulong? FirstWarningTimestamp { get; set; }

        [
            DisplayName("Failed Timestamp"),
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
            DisplayName("Creation Timestamp"),
            Column("creation_timestamp"),
        ]
        public ulong? CreationTimestamp { get; set; }

        [
            DisplayName("Has Warning"),
            Column("warn"),
        ]
        public bool? Warn { get; set; }

        [
            DisplayName("Warning Expire Timestamp"),
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
        public bool? Banned { get; set; }

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

        public bool IsValid(bool ignoreWarning = false, string? groupName = null)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            return string.Compare(GroupName, groupName, true) == 0 &&
                string.IsNullOrEmpty(Failed) || (
                    Failed == "GPR_RED_WARNING" &&
                    (ignoreWarning || (WarnExpireTimestamp ?? ulong.MaxValue) <= now)
                ) || (
                    Failed == "suspended" &&
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
            if (Banned ?? false)
                return "Banned";
            if (FirstWarningTimestamp > 0)
                return "Warning";
            if (string.Compare(Failed, "GPR_RED_WARNING", true) == 0)
                return "Warning";
            if (Warn ?? false)
                return "Warning";
            if (WasSuspended ?? false)
                return "Warning";
            if (string.Compare(Failed, "invalid_credentials", true) == 0)
                return "Invalid";
            // TODO: Cooldown?
            // TODO: InUse?
            return "Good";
        }
    }
}