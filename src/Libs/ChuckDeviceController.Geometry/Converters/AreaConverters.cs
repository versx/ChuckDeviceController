namespace ChuckDeviceController.Geometry.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using ChuckDeviceController.Geometry.Models;

    /// <summary>
    /// Coordinate and geofence value converters
    /// </summary>
    public static class AreaConverters
    {
        /// <summary>
        /// Converts JSON formatted coordinate points column data to dashboard
        /// compatible text value
        /// </summary>
        /// <param name="area">JSON formatted coordinate points</param>
        /// <returns>Returns JSON coordinates as text value</returns>
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

        /// <summary>
        /// Converts JSON formatted geofence column data to dashboard
        /// compatible text value
        /// </summary>
        /// <param name="area">JSON formatted geofence</param>
        /// <returns>Returns JSON geofences as text value</returns>
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

        /// <summary>
        /// Converts coordinate points text as coordinate list
        /// </summary>
        /// <param name="area">Text value of coordinates list</param>
        /// <returns>Returns list of coordinate points</returns>
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

        /// <summary>
        /// Converts geofences text to geofences
        /// </summary>
        /// <param name="area">Text value of geofences list</param>
        /// <returns>Returns list of geofence coordinates</returns>
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
                else if (row.Contains('[') && row.Contains(']') && coords.Count > index && coords[index].Count > 0)
                {
                    coords.Add(new List<Coordinate>());
                    index++;
                }
            }
            return coords;
        }
    }
}