namespace ChuckDeviceController.Data.Extensions;

using POGOProtos.Rpc;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Extensions;

public static class POGOProtosExtensions
{
    public static FortPowerLevelResult GetFortPowerLevel(this PokemonFortProto fortData)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var powerUpLevelExpirationMs = Convert.ToUInt32(fortData.PowerUpLevelExpirationMs / 1000);
        var powerUpPoints = Convert.ToUInt32(fortData.PowerUpProgressPoints);
        ushort? powerUpLevel;
        ulong? powerUpEndTimestamp = null;
        if (fortData.PowerUpProgressPoints < 50)
        {
            powerUpLevel = 0;
        }
        else if (fortData.PowerUpProgressPoints < 100 && powerUpLevelExpirationMs > now)
        {
            powerUpLevel = 1;
            powerUpEndTimestamp = powerUpLevelExpirationMs;
        }
        else if (fortData.PowerUpProgressPoints < 150 && powerUpLevelExpirationMs > now)
        {
            powerUpLevel = 2;
            powerUpEndTimestamp = powerUpLevelExpirationMs;
        }
        else if (powerUpLevelExpirationMs > now)
        {
            powerUpLevel = 3;
            powerUpEndTimestamp = powerUpLevelExpirationMs;
        }
        else
        {
            powerUpLevel = 0;
        }
        var result = new FortPowerLevelResult(powerUpPoints, powerUpLevel, powerUpEndTimestamp);
        return result;
    }
}

public class FortPowerLevelResult : IFortPowerLevel
{
    public uint? PowerUpPoints { get; }

    public ushort? PowerUpLevel { get; }

    public ulong? PowerUpEndTimestamp { get; }

    public FortPowerLevelResult(uint? powerUpPoints, ushort? powerUpLevel, ulong? powerUpEndTimestamp)
    {
        PowerUpPoints = powerUpPoints;
        PowerUpLevel = powerUpLevel;
        PowerUpEndTimestamp = powerUpEndTimestamp;
    }
}