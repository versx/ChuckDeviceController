namespace ChuckDeviceController.Geometry.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using ChuckDeviceController.Geometry.Models;

    public static class AreaConverters
    {
        public static string CoordinatesToAreaString(dynamic area)
        {
            var coords = string.Empty;
            var nfi = new CultureInfo("en-US").NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            foreach (var coord in area.EnumerateArray())
            {
                var latitude = double.Parse(Convert.ToString(coord.GetProperty("lat")), nfi);
                var longitude = double.Parse(Convert.ToString(coord.GetProperty("lon")), nfi);
                coords += $"{latitude},{longitude}\n";
            }
            return coords;
        }

        public static string MultiPolygonToAreaString(dynamic area)
        {
            var index = 1;
            var coords = string.Empty;
            var nfi = new CultureInfo("en-US").NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            foreach (var fence in area.EnumerateArray())
            {
                coords += $"[Geofence {index}]\n";
                foreach (var coord in fence.EnumerateArray())
                {
                    var latitude = double.Parse(Convert.ToString(coord.GetProperty("lat")), nfi);
                    var longitude = double.Parse(Convert.ToString(coord.GetProperty("lon")), nfi);
                    coords += $"{latitude},{longitude}\n";
                }
                index++;
            }
            return coords;
        }

        public static List<Coordinate> AreaStringToCoordinates(string area)
        {
            var rows = area.Split('\n');
            var coords = new List<Coordinate>();
            var nfi = new CultureInfo("en-US").NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            foreach (var row in rows)
            {
                var split = row.Split(',');
                if (split.Length != 2)
                    continue;
                var latitude = double.Parse(split[0].Trim('\n'), nfi);
                var longitude = double.Parse(split[1].Trim('\n'), nfi);
                coords.Add(new Coordinate(latitude, longitude));
            }
            return coords;
        }

        public static List<List<Coordinate>> AreaStringToMultiPolygon(string area)
        {
            var rows = area.Split('\n');
            var index = 0;
            var coords = new List<List<Coordinate>> { new List<Coordinate>() };
            var nfi = new CultureInfo("en-US").NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            foreach (var row in rows)
            {
                var split = row.Split(',');
                if (split.Length == 2)
                {
                    var latitude = double.Parse(split[0].Trim('\0'), nfi);
                    var longitude = double.Parse(split[1].Trim('\0'), nfi);
                    coords[index].Add(new Coordinate(latitude, longitude));
                }
                else if (row.Contains("[") && row.Contains("]") && coords.Count > index && coords[index].Count > 0)
                {
                    coords.Add(new List<Coordinate>());
                    index++;
                }
            }
            return coords;
        }
    }
}