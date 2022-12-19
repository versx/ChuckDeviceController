namespace ChuckDeviceController.Geometry.Models;

using ChuckDeviceController.Geometry.Models.Abstractions;

/// <summary>
/// Square or rectangular bounding box boundary for an area/geofence.
/// </summary>
public class BoundingBox : IBoundingBox
{
    #region Properties

    /// <summary>
    /// Gets or sets the minimum latitude of the bounding box.
    /// </summary>
    public double MinimumLatitude { get; set; } // minX

    /// <summary>
    /// Gets or sets the maximum latitude of the bounding box.
    /// </summary>
    public double MaximumLatitude { get; set; } // maxX

    /// <summary>
    /// Gets or sets the minimum longitude of the bounding box.
    /// </summary>
    public double MinimumLongitude { get; set; } // minY

    /// <summary>
    /// Gets or sets the maximum longitude of the bounding box.
    /// </summary>
    public double MaximumLongitude { get; set; } // maxY

    #endregion

    /// <summary>
    ///     Check if the specified latitude and longitude are within the
    ///     bounding box boundaries.
    /// </summary>
    /// <param name="latitude">
    ///     The latitude of the geocoordinate.
    /// </param>
    /// <param name="longitude">
    ///     The longitude of the geocoordinate.
    /// </param>
    /// <returns>
    ///     Returns <c>true</c> if the coordinate is within the bounding boxes boundaries.
    /// </returns>
    public bool IsInBoundingBox(double latitude, double longitude)
    {
        var result =
            latitude >= MinimumLatitude && longitude >= MinimumLongitude &&
            latitude <= MaximumLatitude && longitude <= MaximumLongitude;
        return result;
    }
}