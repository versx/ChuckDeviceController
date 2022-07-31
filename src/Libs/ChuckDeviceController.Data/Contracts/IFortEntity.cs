namespace ChuckDeviceController.Data.Contracts
{
    public interface IFortEntity
    {
        string Id { get; set; }

        string Name { get; set; }

        string Url { get; set; }

        bool IsEnabled { get; }

        bool IsDeleted { get; }

        ulong CellId { get; }

        uint? PowerUpPoints { get; }

        ushort? PowerUpLevel { get; }

        ulong? PowerUpEndTimestamp { get; }
    }
}