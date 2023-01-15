namespace ChuckDeviceController.Common.Abstractions;

public interface IWeather : IBaseEntity
{
    long Id { get; }

    ushort Level { get; }

    double Latitude { get; }

    double Longitude { get; }

    ushort GameplayCondition { get; }

    ushort WindDirection { get; }

    ushort CloudLevel { get; }

    ushort RainLevel { get; }

    ushort WindLevel { get; }

    ushort SnowLevel { get; }

    ushort FogLevel { get; }

    ushort SpecialEffectLevel { get; }

    ushort? Severity { get; }

    bool? WarnWeather { get; }

    ulong Updated { get; }
}