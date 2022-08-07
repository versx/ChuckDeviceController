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
    }
}