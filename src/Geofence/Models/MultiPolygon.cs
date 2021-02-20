namespace ChuckDeviceController.Geofence.Models
{
    using Google.Common.Geometry;
    using System.Collections.Generic;
    using System.Linq;

    public class MultiPolygon : List<Polygon>
    {
        public List<ulong> GetS2CellIDs(ushort minLevel, ushort maxLevel, int maxCells)
        {
            BoundingBox bbox = GetBoundingBox();
            S2RegionCoverer regionCoverer = new S2RegionCoverer
            {
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                MaxCells = maxCells,
            };
            S2LatLngRect region = S2LatLngRect.FromPointPair(
                S2LatLng.FromDegrees(bbox.MinimumLatitude, bbox.MinimumLongitude),//bbox[1], bbox[0]),
                S2LatLng.FromDegrees(bbox.MaximumLatitude, bbox.MaximumLongitude)//bbox[3], bbox[2])
            );
            S2CellUnion cellIDsBBox = regionCoverer.GetInteriorCovering(region);
            List<ulong> cellIDs = new List<ulong>();
            foreach (S2CellId cellId in cellIDsBBox)
            {
                S2Cell cell = new S2Cell(cellId);
                S2Point vertex0 = cell.GetVertex(0);
                S2Point vertex1 = cell.GetVertex(1);
                S2Point vertex2 = cell.GetVertex(2);
                S2Point vertex3 = cell.GetVertex(3);
                S2LatLng coord0 = new S2LatLng(new S2Point(vertex0.X, vertex0.Y, vertex0.Z));
                S2LatLng coord1 = new S2LatLng(new S2Point(vertex1.X, vertex1.Y, vertex1.Z));
                S2LatLng coord2 = new S2LatLng(new S2Point(vertex2.X, vertex2.Y, vertex2.Z));
                S2LatLng coord3 = new S2LatLng(new S2Point(vertex3.X, vertex3.Y, vertex3.Z));
                if (GeofenceService.InPolygon(this, coord0.LatDegrees, coord0.LngDegrees) ||
                    GeofenceService.InPolygon(this, coord1.LatDegrees, coord1.LngDegrees) ||
                    GeofenceService.InPolygon(this, coord2.LatDegrees, coord2.LngDegrees) ||
                    GeofenceService.InPolygon(this, coord3.LatDegrees, coord3.LngDegrees))
                {
                    cellIDs.Add(cellId.Id);
                }
            }
            return cellIDs;
        }

        public BoundingBox GetBoundingBox()
        {
            // Add checks here, if necessary, to make sure that points is not null,
            // and that it contains at least one (or perhaps two?) elements
            double minX = this.Min(p => p[0]);
            double minY = this.Min(p => p[1]);
            double maxX = this.Max(p => p[0]);
            double maxY = this.Max(p => p[1]);

            return new BoundingBox
            {
                MinimumLatitude = minX,
                MaximumLatitude = maxX,
                MinimumLongitude = minY,
                MaximumLongitude = maxY,
            };//(new Point(minX, minY), new Size(maxX - minX, maxY - minY));
        }
    }
}