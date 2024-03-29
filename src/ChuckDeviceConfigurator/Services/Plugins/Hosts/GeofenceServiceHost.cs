﻿namespace ChuckDeviceConfigurator.Services.Plugins.Hosts;

using System.Threading.Tasks;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Factories;
using ChuckDeviceController.Geometry;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.Plugin;

public class GeofenceServiceHost : IGeofenceServiceHost
{
    #region Variables

    private static readonly ILogger<IGeofenceServiceHost> _logger =
        new Logger<IGeofenceServiceHost>(LoggerFactory.Create(x => x.AddConsole()));
    private readonly string _connectionString;

    #endregion

    #region Constructors

    public GeofenceServiceHost(string connectionString)
    {
        _connectionString = connectionString;
    }

    #endregion

    #region Geofence Methods

    public async Task CreateGeofenceAsync(IGeofence options)
    {
        using var context = DbContextFactory.CreateControllerContext(_connectionString);
        var geofence = new Geofence
        {
            Name = options.Name,
            Type = options.Type,
            Data = new GeofenceData
            {
                Area = options.Data?.Area,
            },
        };

        if (context.Geofences.Any(x => x.Name == options.Name))
        {
            context.Geofences.Update(geofence);
        }
        else
        {
            await context.Geofences.AddAsync(geofence);
        }
        await context.SaveChangesAsync();
    }

    public async Task<IGeofence> GetGeofenceAsync(string name)
    {
        using var context = DbContextFactory.CreateControllerContext(_connectionString);
        var geofence = await context.Geofences.FindAsync(name);
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