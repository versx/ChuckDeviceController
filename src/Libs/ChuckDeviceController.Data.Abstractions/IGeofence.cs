namespace ChuckDeviceController.Data.Abstractions;

using ChuckDeviceController.Data.Common;

public interface IGeofence : IBaseEntity
{
    string Name { get; }

    GeofenceType Type { get; }

    IGeofenceData Data { get; }
}

public interface IGeofenceData
{
    dynamic? Area { get; }
}