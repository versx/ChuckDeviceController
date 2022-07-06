namespace ChuckDeviceConfigurator.Services.Geofences
{
	using ChuckDeviceController.Data.Entities;

	public interface IGeofenceControllerService
	{
		void Load();

        void Reload();

		void AddGeofence(Geofence geofence);

		void EditGeofence(Geofence newGeofence, string oldGeofenceName);

		void DeleteGeofence(string name);

        Geofence GetGeofence(string name);

		IReadOnlyList<Geofence> GetGeofences(IReadOnlyList<string> names);
    }
}