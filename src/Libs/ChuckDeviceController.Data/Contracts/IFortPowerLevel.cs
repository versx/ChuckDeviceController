namespace ChuckDeviceController.Data.Contracts
{
    public interface IFortPowerLevel
    {
        uint? PowerUpPoints { get; }

        ushort? PowerUpLevel { get; }

        ulong? PowerUpEndTimestamp { get; }
    }
}