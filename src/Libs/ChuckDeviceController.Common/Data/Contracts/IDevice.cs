namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IDevice : IBaseEntity
    {
        string Uuid { get; }

        string? InstanceName { get; }

        string? AccountUsername { get; }

        string? LastHost { get; }

        double? LastLatitude { get; }

        double? LastLongitude { get; }

        ulong? LastSeen { get; } // Last job request requested

        // TODO: Add Device LastDataReceived timestamp

        bool IsPendingAccountSwitch { get; } // used internally
    }
}