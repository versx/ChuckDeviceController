namespace ChuckDeviceConfigurator.Services.Plugins.Hosts;

using System.Threading.Tasks;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Geometry;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.Plugin;

public class GeofenceServiceHost : IGeofenceServiceHost
{
    #region Variables

    private static readonly ILogger<IGeofenceServiceHost> _logger =
        new Logger<IGeofenceServiceHost>(LoggerFactory.Create(x => x.AddConsole()));
    private readonly IDapperUnitOfWork _uow;

    #endregion

    #region Constructors

    public GeofenceServiceHost(IDapperUnitOfWork uow)
    {
        _uow = uow;
    }

    #endregion

    #region Geofence Methods

    public async Task CreateGeofenceAsync(IGeofence options)
    {
        var geofence = new Geofence
        {
            Name = options.Name,
            Type = options.Type,
            Data = new GeofenceData
            {
                Area = options.Data?.Area,
            },
        };

        if (_uow.Geofences.Any(x => x.Name == options.Name))
        {
            await _uow.Geofences.UpdateAsync(geofence);
        }
        else
        {
            await _uow.Geofences.InsertAsync(geofence);
        }
    }

    public async Task<IGeofence> GetGeofenceAsync(string name)
    {
        var geofence = await _uow.Geofences.FindAsync(name);
        return geofence;
    }

    #endregion

    #region Geofence Converter Methods

    public (IReadOnlyList<IMultiPolygon>, IReadOnlyList<IReadOnlyList<ICoordinate>>) GetMultiPolygons(IGeofence geofence)
    {
        var (multiPolygons, coords) = geofence.ConvertToMultiPolygons();
        return (multiPolygons, coords);
    }

    public IReadOnlyList<ICoordinate>? GetCoordinates(IGeofence geofence)
    {
        var coords = geofence.ConvertToCoordinates();
        return coords;
    }

    #endregion

    #region Point In Polygon Methods

    public bool IsPointInMultiPolygons(ICoordinate coord, IEnumerable<IMultiPolygon> multiPolygons)
    {
        return GeofenceService.InMultiPolygon(
            multiPolygons.ToList(),
            new Coordinate(coord.Latitude, coord.Longitude)
        );
    }

    public bool IsPointInMultiPolygon(ICoordinate coord, IMultiPolygon multiPolygon)
    {
        return GeofenceService.InPolygon(
            multiPolygon,
            new Coordinate(coord.Latitude, coord.Longitude)
        );
    }

    public bool IsPointInPolygon(ICoordinate coord, IEnumerable<ICoordinate> coordinates)
    {
        return GeofenceService.IsPointInPolygon(
            new Coordinate(coord.Latitude, coord.Longitude),
            coordinates.ToList()
        );
    }

    #endregion
}