namespace ChuckDeviceController.Data.Abstractions;

public interface IFortPowerLevel
{
    uint? PowerUpPoints { get; }

    ushort? PowerUpLevel { get; }

    ulong? PowerUpEndTimestamp { get; }
}