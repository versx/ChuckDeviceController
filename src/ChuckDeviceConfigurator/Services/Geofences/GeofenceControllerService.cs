namespace ChuckDeviceConfigurator.Services.Geofences;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceController.Collections;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;

public class GeofenceControllerService : IGeofenceControllerService
{
    #region Variables

    private readonly ILogger<IGeofenceControllerService> _logger;
    private readonly IDbContextFactory<ControllerDbContext> _factory;

    private SafeCollection<Geofence> _geofences;

    #endregion

    #region Constructor

    public GeofenceControllerService(
        ILogger<IGeofenceControllerService> logger,
        IDbContextFactory<ControllerDbContext> factory)
    {
        _logger = logger;
        _factory = factory;
        _geofences = new();

        Reload();
    }

    #endregion

    #region Public Methods

    public void Reload()
    {
        var geofences = GetGeofences();
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

    private List<Geofence> GetGeofences()
    {
        using var context = _factory.CreateDbContext();
        var geofences = context.Geofences.ToList();
        return geofences;
    }

    #endregion
}