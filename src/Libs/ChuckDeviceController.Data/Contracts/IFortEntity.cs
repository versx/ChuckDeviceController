namespace ChuckDeviceController.Data.Contracts
{
    public interface IFortEntity : IFortPowerLevel
    {
        string Id { get; }

        string? Name { get; }

        string? Url { get; }

        bool IsEnabled { get; }

        bool IsDeleted { get; }

        ulong CellId { get; }

        //bool IsArScanEligible { get; }

        ulong FirstSeenTimestamp { get; }

        ulong LastModifiedTimestamp { get; }

        ulong Updated { get; }
    }
}