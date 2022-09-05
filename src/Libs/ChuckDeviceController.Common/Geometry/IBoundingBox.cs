namespace ChuckDeviceController.Common.Geometry
{
    /// <summary>
    /// Square or rectangular bounding box boundary for an area/geofence.
    /// </summary>
    public interface IBoundingBox
    {
        #region Properties

        /// <summary>
        /// Gets or sets the minimum latitude of the bounding box.
        /// </summary>
        double MinimumLatitude { get; } // minX

        /// <summary>
        /// Gets or sets the maximum latitude of the bounding box.
        /// </summary>
        double MaximumLatitude { get; } // maxX

        /// <summary>
        /// Gets or sets the minimum longitude of the bounding box.
        /// </summary>
        double MinimumLongitude { get; } // minY

        /// <summary>
        /// Gets or sets the maximum longitude of the bounding box.
        /// </summary>
        double MaximumLongitude { get; } // maxY

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
        bool IsInBoundingBox(double latitude, double longitude);
    }
}