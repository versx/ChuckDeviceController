namespace ChuckDeviceConfigurator.Services.Geofences
{
	using ChuckDeviceController.Data.Entities;

	/// <summary>
	/// Caches all configured geofences to reduce database loads.
	/// </summary>
	public interface IGeofenceControllerService : IControllerService<Geofence, string>
	{
    }
}