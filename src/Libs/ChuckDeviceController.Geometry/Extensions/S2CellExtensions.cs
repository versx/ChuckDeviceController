namespace ChuckDeviceController.Geometry.Extensions
{
    using Google.Common.Geometry;

    using ChuckDeviceController.Common.Geometry;
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
            var coverer = new S2RegionCoverer
            {
                MaxCells = 100,
                MinLevel = 15,
                MaxLevel = 15,
            };
            var s2cells = coverer.GetCovering(circle);
            var list = s2cells.ToList();
            return list;
        }

        public static List<Coordinate> GetS2CellCoordinates(this IBoundingBox bbox, ushort minLevel = 15, ushort maxLevel = 15, int maxCells = 100)
        {
            var cellIds = GetS2CellCoverage(bbox, minLevel, maxLevel, maxCells);
            var coordinates = cellIds.Select(cell => cell.ToLatLng())
                                     .Select(cell => new Coordinate(cell.LatDegrees, cell.LngDegrees))
                                     .ToList();
            return coordinates;
        }

        public static Coordinate ToCoordinate(this long cellId) =>
            ToCoordinate((ulong)cellId);
        public static Coordinate ToCoordinate(this ulong cellId)
        {
            var latlng = S2LatLngFromId(cellId);
            var coord = new Coordinate(latlng.LatDegrees, latlng.LngDegrees);
            return coord;
        }

        public static Coordinate ToCoordinate(this S2CellId cellId)
        {
            var latlng = cellId.ToLatLng();
            var coord = new Coordinate(latlng.LatDegrees, latlng.LngDegrees);
            return coord;
        }

        public static S2CellUnion GetS2CellCoverage(this IBoundingBox bbox, ushort minLevel = 15, ushort maxLevel = 15, int maxCells = int.MaxValue)
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

        public static S2LatLngRect GetS2Region(this IBoundingBox bbox)
        {
            var min = S2LatLng.FromDegrees(bbox.MinimumLatitude, bbox.MinimumLongitude);
            var max = S2LatLng.FromDegrees(bbox.MaximumLatitude, bbox.MaximumLongitude);
            var rect = new S2LatLngRect(min, max);
            return rect;
        }

        public static Coordinate ToCoordinate(this S2Point point)
        {
            var latlng = new S2LatLng(point);
            var coord = new Coordinate(latlng.LatDegrees, latlng.LngDegrees);
            return coord;
        }
    }
}