namespace ChuckDeviceController.Geometry.Models;

using System.Collections.Generic;
using System.Linq;

using ChuckDeviceController.Geometry.Models.Abstractions;

public class MultiPolygon : List<IPolygon>, IMultiPolygon
{
    /// <summary>
    /// Gets the S2 cells within the current multi polygon
    /// </summary>
    /// <param name="minLevel">Minimum S2 cell level to meet</param>
    /// <param name="maxLevel">Maximum S2 cell level to meet</param>
    /// <param name="maxCells">Maximum S2 cells to return</param>
    /// <returns>Returns a list of <seealso cref="S2CellId"/> objects</returns>
    //public IReadOnlyList<ulong> GetS2CellIds(ushort minLevel = 15, ushort maxLevel = 15, int maxCells = ushort.MaxValue)
    //{
    //    var bbox = GetBoundingBox();
    //    var result = new List<ulong>();
    //    var coverage = bbox.GetS2CellCoverage(minLevel, maxLevel, maxCells);
    //    foreach (var cellId in coverage)
    //    {
    //        var cell = new S2Cell(cellId);
    //        for (var i = 0; i <= 3; i++)
    //        {
    //            var vertex = cell.GetVertex(i);
    //            var coord = vertex.ToCoordinate();
    //            if (!GeofenceService.InPolygon(this, coord))
    //                continue;

    //            result.Add(cellId.Id);
    //        }
    //    }
    //    return result;
    //}

    /// <summary>
    /// Converts multi polygon geofence to list of coordinates
    /// </summary>
    /// <returns>Returns list of coordinates for geofence</returns>
    public IReadOnlyList<ICoordinate> ToCoordinates()
    {
        var coords = this.Select(polygon => (ICoordinate)new Coordinate(polygon[0], polygon[1]))
                         .ToList();
        return coords;
    }

    /// <summary>
    /// Gets the bounding box boundaries of the multi polygon geofence
    /// </summary>
    /// <returns>Returns the bounding box of the multi polygon geofence</returns>
    public IBoundingBox GetBoundingBox()
    {
        // Add checks here, if necessary, to make sure that points is not null,
        // and that it contains at least one (or perhaps two?) elements
        var minX = this.Min(p => p[0]);
        var minY = this.Min(p => p[1]);
        var maxX = this.Max(p => p[0]);
        var maxY = this.Max(p => p[1]);
        return new BoundingBox
        {
            MinimumLatitude = minX,
            MaximumLatitude = maxX,
            MinimumLongitude = minY,
            MaximumLongitude = maxY,
        };
    }
}