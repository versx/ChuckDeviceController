namespace ChuckDeviceController.Data.Contracts
{
    public interface IFortEntity
    {
        bool IsEnabled { get; }

        bool IsDeleted { get; }

        ulong CellId { get; }

        uint? PowerUpPoints { get; }

        ushort? PowerUpLevel { get; }

        ulong? PowerUpEndTimestamp { get; }
    }
}