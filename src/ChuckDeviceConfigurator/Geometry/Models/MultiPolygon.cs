namespace ChuckDeviceConfigurator.Geometry.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Google.Common.Geometry;

    public class MultiPolygon : List<Polygon>
    {
        public List<S2CellId> GetS2CellIds(ushort level, int maxCells)
        {
            //var geofence = Geofence.FromMultiPolygon(this);
            var bbox = GetBoundingBox();
            var regionCoverer = new S2RegionCoverer
            {
                MinLevel = level,
                MaxLevel = level,
                MaxCells = maxCells,
            };
            var region = new S2LatLngRect(
                S2LatLng.FromDegrees(bbox.MinimumLatitude, bbox.MinimumLongitude),
                S2LatLng.FromDegrees(bbox.MaximumLatitude, bbox.MaximumLongitude)
            );
            var coverage = new List<S2CellId>();
            regionCoverer.GetCovering(region, coverage);
            var result = new List<S2CellId>();
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