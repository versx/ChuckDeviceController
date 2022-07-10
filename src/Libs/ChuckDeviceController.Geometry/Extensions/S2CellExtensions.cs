namespace ChuckDeviceController.Geometry.Extensions
{
    using Google.Common.Geometry;

    using ChuckDeviceController.Geometry.Models;

    public static class S2CellExtensions
    {
        public static S2CellId S2CellIdFromCoordinate(this Coordinate coord) =>
            S2CellIdFromLatLng(coord.Latitude, coord.Longitude);

        public static S2CellId S2CellIdFromLatLng(double latitude, double longitude)
        {
            var latlng = S2LatLng.FromDegrees(latitude, longitude);
            var s2cellId = S2CellId.FromLatLng(latlng);
            return s2cellId;
        }

        public static S2Cell S2CellFromId(this long cellId) =>
            S2CellFromId((ulong)cellId);
        public static S2Cell S2CellFromId(this ulong cellId)
        {
            var s2cellId = new S2CellId(cellId);
            var s2cell = new S2Cell(s2cellId);
            return s2cell;
        }

        public static S2LatLng S2LatLngFromId(this long cellId) =>
            S2LatLngFromId((ulong)cellId);
        public static S2LatLng S2LatLngFromId(this ulong cellId)
        {
            var s2cell = S2CellFromId(cellId);
            var center = s2cell.RectBound.Center;
            return center;
        }

        public static Coordinate CoordinateFromS2CellId(this long cellId) =>
            CoordinateFromS2CellId((ulong)cellId);
        public static Coordinate CoordinateFromS2CellId(this ulong cellId)
        {
            var latlng = S2LatLngFromId(cellId);
            var coord = new Coordinate(latlng.LatDegrees, latlng.LngDegrees);
            return coord;
        }

        public static List<S2CellId> GetLoadedS2CellIds(this S2LatLng latlng)
        {
            double radius;
            if (latlng.LatDegrees <= 39)
                radius = 715;
            else if (latlng.LatDegrees >= 69)
                radius = 330;
            else
                radius = -13 * latlng.LatDegrees + 1225;

            var radians = radius / 6378137;
            var centerNormalizedPoint = latlng.Normalized.ToPoint();
            var circle = S2Cap.FromAxisHeight(centerNormalizedPoint, (radians * radians) / 2);
            var coverer = new S2RegionCoverer();
            coverer.MaxCells = 100;
            coverer.MinLevel = 15;
            coverer.MaxLevel = 15;
            var s2cells = coverer.GetCovering(circle);
            var list = s2cells.ToList();
            return list;
        }

        public static List<Coordinate> GetS2CellCoordinates(this BoundingBox bbox, ushort minLevel = 15, ushort maxLevel = 15, ushort maxCells = 100)
        {
            var regionCoverer = new S2RegionCoverer
            {
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                MaxCells = maxCells,
            };
            var region = bbox.GetS2Region();
            var cellIds = regionCoverer.GetCovering(region);
            var coordinates = new List<Coordinate>();
            foreach (var cellId in cellIds)
            {
                var center = cellId.ToLatLng();
                coordinates.Add(new Coordinate(center.LatDegrees, center.LngDegrees));
            }
            return coordinates;
        }

        public static S2CellUnion GetS2CellCoverage(this BoundingBox bbox, ushort minLevel = 15, ushort maxLevel = 15, int maxCells = int.MaxValue)
        {
            var regionCoverer = new S2RegionCoverer
            {
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                MaxCells = maxCells,
            };
            var region = bbox.GetS2Region();
            var coverage = regionCoverer.GetCovering(region);
            return coverage;
        }

        public static S2LatLngRect GetS2Region(this BoundingBox bbox)
        {
            var min = S2LatLng.FromDegrees(bbox.MinimumLatitude, bbox.MinimumLongitude);
            var max = S2LatLng.FromDegrees(bbox.MaximumLatitude, bbox.MaximumLongitude);
            var rect = new S2LatLngRect(max, min);
            return rect;
        }
    }
}