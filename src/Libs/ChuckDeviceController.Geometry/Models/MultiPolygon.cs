namespace ChuckDeviceController.Geometry.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Google.Common.Geometry;

    using ChuckDeviceController.Geometry.Extensions;

    public class MultiPolygon : List<Polygon>
    {
        public List<S2CellId> GetS2CellIds(ushort minLevel, ushort maxLevel, int maxCells)
        {
            var bbox = GetBoundingBox();
            var result = new List<S2CellId>();
            var coverage = bbox.GetS2CellCoverage(minLevel, maxLevel, maxCells);
            foreach (var cellId in coverage)
            {
                var cell = new S2Cell(cellId);
                for (var i = 0; i <= 3; i++)
                {
                    var vertex = cell.GetVertex(i);
                    var coord = new S2LatLng(new S2Point(vertex.X, vertex.Y, vertex.Z));
                    //if (geofence.Intersects(coord.LatDegrees, coord.LngDegrees))
                    if (GeofenceService.InPolygon(this, coord.LatDegrees, coord.LngDegrees))
                    {
                        result.Add(cellId);
                    }
                }
            }
            return result;
        }

        public List<Coordinate> ConvertToCoordinates()
        {
            var coords = this.Select(polygon => new Coordinate(polygon[0], polygon[1]))
                             .ToList();
            return coords;
        }

        public BoundingBox GetBoundingBox()
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
}