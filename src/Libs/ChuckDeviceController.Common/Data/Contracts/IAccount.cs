namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IAccount : IBaseEntity
    {
        string Username { get; }

        string Password { get; }

        ushort Level { get; }

        ulong? FirstWarningTimestamp { get; }

        ulong? FailedTimestamp { get; }

        string? Failed { get; }

        ulong? LastEncounterTime { get; }

        double? LastEncounterLatitude { get; }

        double? LastEncounterLongitude { get; }

        uint Spins { get; }

        ushort Tutorial { get; }

        ulong? CreationTimestamp { get; }

        bool? HasWarn { get; }

        ulong? WarnExpireTimestamp { get; }

        bool? WarnMessageAcknowledged { get; }

        bool? SuspendedMessageAcknowledged { get; }

        bool? WasSuspended { get; }

        bool? IsBanned { get; }

        ulong? LastUsedTimestamp { get; }

        string? GroupName { get; }
    }
}