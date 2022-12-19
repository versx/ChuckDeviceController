namespace ChuckDeviceController.Data.Abstractions;

public interface ICell : IBaseEntity
{
    ulong Id { get; }

    ushort Level { get; }

    double Latitude { get; }

    double Longitude { get; }

    ulong Updated { get; }
}