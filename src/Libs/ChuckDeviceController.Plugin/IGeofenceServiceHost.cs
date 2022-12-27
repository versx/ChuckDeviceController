namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Geometry.Models.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IGeofenceServiceHost
{
    /// <summary>
    /// Create a new or update an existing geofence.
    /// </summary>
    /// <param name="options">Geofence options used to create or update.</param>
    Task CreateGeofenceAsync(IGeofence options);

    /// <summary>
    /// Retrieves a geofence from the database by name.
    /// </summary>
    /// <param name="name">Name of geofence to retrieve.</param>
    /// <returns>Returns a geofence interface contract.</returns>
    Task<IGeofence> GetGeofenceAsync(string name);

    /// <summary>
    /// Gets the geofence boundaries in multipolygon format as well as a two-dimensional list of coordinates.
    /// </summary>
    /// <param name="geofence">Geofence to get coordinates from.</param>
    /// <returns>Returns a tuple with a list of MultiPolygons and a two-dimensional list of coordinates.</returns>
    (IReadOnlyList<IMultiPolygon>, IReadOnlyList<IReadOnlyList<ICoordinate>>) GetMultiPolygons(IGeofence geofence);

    /// <summary>
    /// Gets the geofence location plots as a list of coordinates.
    /// </summary>
    /// <param name="geofence">Geofence to get coordinates from.</param>
    /// <returns>Returns a list of coordinates.</returns>
    IReadOnlyList<ICoordinate>? GetCoordinates(IGeofence geofence);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="multiPolygons"></param>
    /// <returns></returns>
    bool IsPointInMultiPolygons(ICoordinate coord, IEnumerable<IMultiPolygon> multiPolygons);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="multiPolygon"></param>
    /// <returns></returns>
    bool IsPointInMultiPolygon(ICoordinate coord, IMultiPolygon multiPolygon);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="coordinates"></param>
    /// <returns></returns>
    bool IsPointInPolygon(ICoordinate coord, IEnumerable<ICoordinate> coordinates);
}