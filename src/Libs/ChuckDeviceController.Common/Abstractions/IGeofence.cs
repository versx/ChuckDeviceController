namespace ChuckDeviceController.Common.Abstractions;

public interface IGeofence : IBaseEntity
{
    string Name { get; }

    GeofenceType Type { get; }

    GeofenceData? Data { get; }
}