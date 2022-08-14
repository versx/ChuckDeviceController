namespace ChuckDeviceController.Common.Data.Contracts
{
    /// <summary>
    /// Device model entity contract.
    /// </summary>
    public interface IDevice : IBaseEntity
    {
        string Uuid { get; }

        string? InstanceName { get; }

        string? AccountUsername { get; }

        string? LastHost { get; }

        double? LastLatitude { get; }

        double? LastLongitude { get; }

        ulong? LastSeen { get; } // Last job request requested

        bool IsPendingAccountSwitch { get; } // used internally
    }
}