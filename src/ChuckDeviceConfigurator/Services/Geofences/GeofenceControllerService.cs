namespace ChuckDeviceConfigurator.Services.Geofences
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class GeofenceControllerService : IGeofenceControllerService
	{
        #region Variables

        private readonly ILogger<IGeofenceControllerService> _logger;
        private readonly IDbContextFactory<DeviceControllerContext> _factory;

		private readonly object _geofencesLock = new();
        private List<Geofence> _geofences;

        #endregion

        #region Constructor

        public GeofenceControllerService(
			ILogger<IGeofenceControllerService> logger,
			IDbContextFactory<DeviceControllerContext> factory)
		{
			_logger = logger;
			_factory = factory;

			Load();
		}

        #endregion

        #region Public Methods

        public void Load()
		{
			lock (_geofencesLock)
            {
				_geofences = GetGeofences();
            }
		}

		public void AddGeofence(Geofence geofence)
		{
			lock (_geofencesLock)
			{
				if (_geofences.Contains(geofence))
				{
					// Already exists
					return;
				}
				_geofences.Add(geofence);
			}
		}

		public void EditGeofence(Geofence newGeofence, string oldGeofenceName)
		{
			DeleteGeofence(oldGeofenceName);
			AddGeofence(newGeofence);
		}

		public void DeleteGeofence(string name)
		{
			lock (_geofencesLock)
			{
				_geofences = _geofences.Where(x => x.Name != name).ToList();
			}
		}

		public Geofence GetGeofence(string name)
		{
			Geofence? geofence = null;
			lock (_geofencesLock)
			{
				geofence = _geofences.Find(x => x.Name == name);
			}
			return geofence;
		}

		public IReadOnlyList<Geofence> GetGeofences(IReadOnlyList<string> names)
        {
			return names.Select(name => GetGeofence(name))
						.ToList();
        }

		public void Reload()
		{
			lock (_geofencesLock)
			{
				_geofences = GetGeofences();
			}
		}

        #endregion

        #region Private Methods

        private List<Geofence> GetGeofences()
		{
			using (var context = _factory.CreateDbContext())
            {
				var geofences = context.Geofences.ToList();
				return geofences;
            }
		}

		#endregion
	}
}