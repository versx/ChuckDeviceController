namespace ChuckDeviceController.Common.Abstractions;

public interface ISpawnpoint : IBaseEntity
{
    ulong Id { get; }

    double Latitude { get; }

    double Longitude { get; }

    uint? DespawnSecond { get; }

    ulong Updated { get; }

    ulong? LastSeen { get; }
}