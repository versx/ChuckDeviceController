namespace ChuckDeviceConfigurator.Services.Geofences;

using ChuckDeviceController.Collections;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories.Dapper;

public class GeofenceControllerService : IGeofenceControllerService
{
    #region Variables

    private readonly ILogger<IGeofenceControllerService> _logger;
    private readonly IDapperUnitOfWork _uow;

    private SafeCollection<Geofence> _geofences;

    #endregion

    #region Constructor

    public GeofenceControllerService(
        ILogger<IGeofenceControllerService> logger,
        IDapperUnitOfWork uow)
    {
        _logger = logger;
        _uow = uow;
        _geofences = new();

        Reload();
    }

    #endregion

    #region Public Methods

    public void Reload()
    {
        var geofences = GetGeofencesAsync().Result;
        _geofences = new(geofences);
    }

    public void Add(Geofence geofence)
    {
        if (_geofences.Contains(geofence))
        {
            // Already exists
            return;
        }
        if (!_geofences.TryAdd(geofence))
        {
            _logger.LogError($"Failed to add geofence with name '{geofence.Name}'");
        }
    }

    public void Edit(Geofence newGeofence, string oldGeofenceName)
    {
        Delete(oldGeofenceName);
        Add(newGeofence);
    }

    public void Delete(string name)
    {
        if (!_geofences.Remove(x => x.Name == name))
        {
            _logger.LogError($"Failed to remove geofence with name '{name}'");
        }
    }

    public Geofence GetByName(string name)
    {
        var geofence = _geofences.TryGet(x => x.Name == name);
        return geofence;
    }

    public IReadOnlyList<Geofence> GetByNames(IReadOnlyList<string> names)
    {
        var geofences = names
            .Select(GetByName)
            .ToList();
        return geofences;
    }

    #endregion

    #region Private Methods

    private async Task<IEnumerable<Geofence>> GetGeofencesAsync()
    {
        var geofences = await _uow.Geofences.FindAllAsync();
        return geofences;
    }

    #endregion
}