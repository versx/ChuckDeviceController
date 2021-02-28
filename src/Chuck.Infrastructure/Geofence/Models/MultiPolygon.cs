namespace Chuck.Infrastructure.Geofence.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Google.Common.Geometry;

    public class MultiPolygon : List<Polygon>
    {
        public List<S2CellId> GetS2CellIDs(ushort minLevel, ushort maxLevel, int maxCells)
        {
            var bbox = GetBoundingBox();
            var regionCoverer = new S2RegionCoverer
            {
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                MaxCells = maxCells,
            };
            var region = new S2LatLngRect(
                S2LatLng.FromDegrees(bbox.MinimumLatitude, bbox.MinimumLongitude),
                S2LatLng.FromDegrees(bbox.MaximumLatitude, bbox.MaximumLongitude)
            );
            var cellIDsBBox = regionCoverer.GetInteriorCovering(region);
            var cellIDs = new List<S2CellId>();
            foreach (var cellId in cellIDsBBox)
            {
                var cell = new S2Cell(cellId);
                var vertex0 = cell.GetVertex(0);
                var vertex1 = cell.GetVertex(1);
                var vertex2 = cell.GetVertex(2);
                var vertex3 = cell.GetVertex(3);
                var coord0 = new S2LatLng(new S2Point(vertex0.X, vertex0.Y, vertex0.Z));
                var coord1 = new S2LatLng(new S2Point(vertex1.X, vertex1.Y, vertex1.Z));
                var coord2 = new S2LatLng(new S2Point(vertex2.X, vertex2.Y, vertex2.Z));
                var coord3 = new S2LatLng(new S2Point(vertex3.X, vertex3.Y, vertex3.Z));
                if (GeofenceService.InPolygon(this, coord0.LatDegrees, coord0.LngDegrees) ||
                    GeofenceService.InPolygon(this, coord1.LatDegrees, coord1.LngDegrees) ||
                    GeofenceService.InPolygon(this, coord2.LatDegrees, coord2.LngDegrees) ||
                    GeofenceService.InPolygon(this, coord3.LatDegrees, coord3.LngDegrees))
                {
                    cellIDs.Add(cellId);
                }
            }
            return cellIDs;
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