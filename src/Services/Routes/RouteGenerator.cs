namespace ChuckDeviceController.Services.Routes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NetTopologySuite.Geometries;

    using Coordinate = ChuckDeviceController.Data.Entities.Coordinate;
    using ChuckDeviceController.Geofence.Models;

    public class RouteGenerator
    {
        #region Singleton

        private static RouteGenerator _instance;
        public static RouteGenerator Instance
        {
            get
            {
                return _instance ??= new RouteGenerator();
            }
        }

        #endregion

        public List<Coordinate> GenerateBootstrapRoute(List<Geofence> geofences, double circleSize = 70)
        {
            var list = new List<Coordinate>();
            geofences.ForEach(geofence =>
                list.AddRange(GenerateBootstrapRoute(geofence, circleSize)));
            return list;
        }

        public List<Coordinate> GenerateBootstrapRoute(Geofence geofence, double circleSize = 70)
        {
            var geometryFactory = GeometryFactory.Default;
            var xMod = Math.Sqrt(0.75);
            var yMod = Math.Sqrt(0.568);
            var points = new List<Coordinate>();

            var polygon = geofence.Feature.Geometry.Coordinates;
            var line = geometryFactory.CreateLineString(polygon);
            var coords = geofence.BBox.Coordinates;
            var minLat = coords.Min(x => x.X);
            var minLon = coords.Min(x => x.Y);
            var maxLat = coords.Max(x => x.X);
            var maxLon = coords.Max(x => x.Y);
            var currentLatLng = new NetTopologySuite.Geometries.Coordinate(maxLat, maxLon);
            var lastLatLng = new NetTopologySuite.Geometries.Coordinate(minLat, minLon);
            var startLatLng = Destination(currentLatLng, 90, circleSize * 1.5);
            var endLatLng = Destination(Destination(lastLatLng, 270, circleSize * 1.5), 180, circleSize);
            var row = 0;
            var heading = 270;
            var i = 0;
            while (currentLatLng.X > endLatLng.X)
            {
                do
                {
                    var point = new Point(currentLatLng);
                    var distance = point.Distance(line);
                    if (distance <= circleSize || distance == 0 || polygon.Contains(currentLatLng))
                    {
                        points.Add(new Coordinate(currentLatLng.X, currentLatLng.Y));
                    }
                    currentLatLng = Destination(currentLatLng, heading, (xMod * circleSize * 2));
                    i++;
                } while ((heading == 270 && currentLatLng.Y > endLatLng.Y) || (heading == 90 && currentLatLng.Y < startLatLng.Y));

                currentLatLng = Destination(currentLatLng, 180, yMod * circleSize * 2);
                heading = row % 2 == 1
                    ? 270
                    : 90;
                currentLatLng = Destination(currentLatLng, heading, xMod * circleSize * 3);
                row++;
            }
            return points;
        }

        public List<Coordinate> GenerateRandomRoute(Geofence geofence, int maxPoints = 3000, double circleSize = 70)
        {
            var coords = geofence.BBox.Coordinates;
            return Calculate
            (
                new Coordinate(coords[0].X, coords[0].Y),
                new Coordinate(coords[1].X, coords[1].Y),
                new Coordinate(coords[2].X, coords[2].Y),
                new Coordinate(coords[3].X, coords[3].Y),
                maxPoints,
                circleSize
            );
        }

        private static List<Coordinate> Calculate(Coordinate location1, Coordinate location2, Coordinate location3, Coordinate location4, int maxPoints = 3000, double circleSize = 70)
        {
            var allCoords = new List<Coordinate> { location1, location2, location3, location4 };
            double minLat = allCoords.Min(x => x.Latitude);
            double minLon = allCoords.Min(x => x.Longitude);
            double maxLat = allCoords.Max(x => x.Latitude);
            double maxLon = allCoords.Max(x => x.Longitude);

            var r = new Random();
            var result = new List<Coordinate>();
            for (var i = 0; i < maxPoints; i++)
            {
                var point = new Coordinate();
                do
                {
                    //point.Latitude = r.NextDouble() * (maxLat - minLat) + minLat;
                    //point.Longitude = r.NextDouble() * (maxLon - minLon) + minLon;
                    point.Latitude = r.NextDouble() * ((maxLat - minLat) + circleSize / 270) + minLat;
                    point.Longitude += r.NextDouble() * ((maxLon - minLon) + circleSize / 270) + minLon;
                } while (!IsPointInPolygon(point, allCoords));
                result.Add(point);
            }
            result.Sort((a, b) => a.Latitude.CompareTo(b.Latitude));
            return result;
        }

        //took it from http://codereview.stackexchange.com/a/108903
        //you can use your own one
        private static bool IsPointInPolygon(Coordinate point, List<Coordinate> polygon)
        {
            int polygonLength = polygon.Count, i = 0;
            bool inside = false;
            // x, y for tested point.
            double pointX = point.Longitude, pointY = point.Latitude;
            // start / end point for the current polygon segment.
            double startX, startY, endX, endY;
            Coordinate endPoint = polygon[polygonLength - 1];
            endX = endPoint.Longitude;
            endY = endPoint.Latitude;
            while (i < polygonLength)
            {
                startX = endX;
                startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.Longitude;
                endY = endPoint.Latitude;
                //
                inside ^= ((endY > pointY) ^ (startY > pointY)) /* ? pointY inside [startY;endY] segment ? */
                          && /* if so, test if it is under the segment */
                          (pointX - endX < (pointY - endY) * (startX - endX) / (startY - endY));
            }
            return inside;
        }

        /// <summary>
        /// Returns the point that is a distance and heading away from
        /// the given origin point.
        /// </summary>
        /// <param name="latlng">Origin coordinate</param>
        /// <param name="heading">Heading in degrees, clockwise from 0 degrees north.</param>
        /// <param name="distance">Distance in meters</param>
        /// <returns>The destination coordinate</returns>
        private static NetTopologySuite.Geometries.Coordinate Destination(NetTopologySuite.Geometries.Coordinate latlng, double heading, double distance)
        {
            heading = (heading + 360) % 360;
            var rad = Math.PI / 180;
            var radInv = 180 / Math.PI;
            var r = 6378137; // approximation of Earth's radius
            var lon1 = latlng.Y * rad;
            var lat1 = latlng.X * rad;
            var rheading = heading * rad;
            var sinLat1 = Math.Sin(lat1);
            var cosLat1 = Math.Cos(lat1);
            var cosDistR = Math.Cos(distance / r);
            var sinDistR = Math.Sin(distance / r);
            var lat2 = Math.Asin(sinLat1 * cosDistR + cosLat1 *
                    sinDistR * Math.Cos(rheading));
            var lon2 = lon1 + Math.Atan2(Math.Sin(rheading) * sinDistR *
                    cosLat1, cosDistR - sinLat1 * Math.Sin(lat2));
            lon2 *= radInv;
            lon2 = lon2 > 180 ? lon2 - 360 : lon2 < -180 ? lon2 + 360 : lon2;
            return new NetTopologySuite.Geometries.Coordinate(lat2 * radInv, lon2);
        }
    }
}