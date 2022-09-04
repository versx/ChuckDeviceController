namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;

    public interface IGeofenceCreationOptions
    {
        string Name { get; }

        GeofenceType Type { get; }

        IGeofenceData? Data { get; }
    }
}