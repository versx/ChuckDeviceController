namespace ChuckDeviceController.JobControllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Chuck.Data.Entities;
    using Chuck.Data.Factories;
    using Chuck.Data.Repositories;

    public class GeofenceController
    {
        #region Variables

        //private readonly ILogger<IVListController> _logger;

        private readonly IDictionary<string, Geofence> _geofences;
        private readonly GeofenceRepository _geofenceRepository;

        private readonly object _geofencesLock = new object();

        #endregion

        #region Singleton

        private static GeofenceController _instance;
        public static GeofenceController Instance =>
            _instance ??= new GeofenceController();

        #endregion

        public GeofenceController()
        {
            _geofences = new Dictionary<string, Geofence>();
            _geofenceRepository = new GeofenceRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            //_logger = new Logger<IVListController>(LoggerFactory.Create(x => x.AddConsole()));
            Reload().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task Reload()
        {
            var geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
            lock (_geofencesLock)
            {
                _geofences.Clear();
                foreach (var geofence in geofences)
                {
                    if (!_geofences.ContainsKey(geofence.Name))
                    {
                        _geofences.Add(geofence.Name, geofence);
                    }
                }
            }
        }

        public Geofence GetGeofence(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (!_geofences.ContainsKey(name))
            {
                return null;
            }
            return _geofences[name];
        }

        public List<Geofence> GetGeofences(List<string> names)
        {
            return names.Select(x => GetGeofence(x)).ToList();
        }
    }
}