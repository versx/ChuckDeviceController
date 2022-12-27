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
    /// 
    /// </summary>
    /// <param name="geofence"></param>
    /// <returns></returns>
    (IReadOnlyList<IMultiPolygon>, IReadOnlyList<IReadOnlyList<ICoordinate>>) ConvertToMultiPolygons(IGeofence geofence);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="geofence"></param>
    /// <returns></returns>
    IReadOnlyList<ICoordinate>? ConvertToCoordinates(IGeofence geofence);

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