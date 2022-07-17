namespace ChuckDeviceConfigurator.Services.Geofences
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class GeofenceControllerService : IGeofenceControllerService
	{
        #region Variables

        //private readonly ILogger<IGeofenceControllerService> _logger;
        private readonly IDbContextFactory<DeviceControllerContext> _factory;

		private readonly object _geofencesLock = new();
        private List<Geofence> _geofences;

        #endregion

        #region Constructor

        public GeofenceControllerService(
			//ILogger<IGeofenceControllerService> logger,
			IDbContextFactory<DeviceControllerContext> factory)
		{
			//_logger = logger;
			_factory = factory;
			_geofences = new();

			Reload();
		}

        #endregion

        #region Public Methods

        public void Reload()
		{
			lock (_geofencesLock)
            {
				_geofences = GetGeofences();
            }
		}

		public void Add(Geofence geofence)
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

		public void Edit(Geofence newGeofence, string oldGeofenceName)
		{
			Delete(oldGeofenceName);
			Add(newGeofence);
		}

		public void Delete(string name)
		{
			lock (_geofencesLock)
			{
				_geofences = _geofences.Where(x => x.Name != name)
									   .ToList();
			}
		}

		public Geofence GetByName(string name)
		{
			Geofence? geofence = null;
			lock (_geofencesLock)
			{
				geofence = _geofences.Find(x => x.Name == name);
			}
			return geofence;
		}

		public IReadOnlyList<Geofence> GetByNames(IReadOnlyList<string> names)
        {
			return names.Select(name => GetByName(name))
						.ToList();
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