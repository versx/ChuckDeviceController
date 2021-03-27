namespace Chuck.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Chuck.Data.Interfaces;
    using Chuck.Extensions;

    [Table("account")]
    public class Account : BaseEntity, IAggregateRoot
    {
        [
            Column("username"),
            Key,
        ]
        public string Username { get; set; }

        [
            Column("password"),
            Required,
        ]
        public string Password { get; set; }

        [Column("first_warning_timestamp")]
        public ulong? FirstWarningTimestamp { get; set; }

        [Column("failed_timestamp")]
        public ulong? FailedTimestamp { get; set; }

        [Column("failed")]
        public string Failed { get; set; }

        [Column("level")]
        public ushort Level { get; set; }

        [Column("last_encounter_time")]
        public ulong? LastEncounterTime { get; set; }

        [Column("last_encounter_lat")]
        public double? LastEncounterLatitude { get; set; }

        [Column("last_encounter_lon")]
        public double? LastEncounterLongitude { get; set; }

        [Column("spins")]
        public uint Spins { get; set; }

        [Column("tutorial")]
        public ushort Tutorial { get; set; }

        [Column("creation_timestamp")]
        public ulong? CreationTimestamp { get; set; }

        [Column("warn")]
        public bool? Warn { get; set; }

        [Column("warn_expire_timestamp")]
        public ulong? WarnExpireTimestamp { get; set; }

        [Column("warn_message_acknowledged")]
        public bool? WarnMessageAcknowledged { get; set; }

        [Column("suspended_message_acknowledged")]
        public bool? SuspendedMessageAcknowledged { get; set; }

        [Column("was_suspended")]
        public bool? WasSuspended { get; set; }

        [Column("banned")]
        public bool? Banned { get; set; }

        [Column("last_used_timestamp")]
        public ulong? LastUsedTimestamp { get; set; } = 0;

        [Column("group")]
        public string GroupName { get; set; }

        public bool IsValid(bool ignoreWarning = false, string groupName = null)
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