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
    }
}