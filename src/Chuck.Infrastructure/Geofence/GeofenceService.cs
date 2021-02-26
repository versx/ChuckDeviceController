namespace Chuck.Infrastructure.Geofence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Geofence.Models;

    public static class GeofenceService
    {
        public static bool InMultiPolygon(List<MultiPolygon> multiPolygon, double latitude, double longitude)
        {
            foreach (var polygon in multiPolygon)
            {
                if (InPolygon(polygon, latitude, longitude))
                    return true;
            }
            return false;
        }

        public static bool InPolygon(MultiPolygon polygon, double latitude, double longitude)
        {
            var numOfPoints = polygon.Count;
            var lats = polygon.Select(x => x[0]).ToList();
            var lngs = polygon.Select(x => x[1]).ToList();
            var polygonContainsPoint = false;
            for (int node = 0, altNode = (numOfPoints - 1); node < numOfPoints; altNode = node++)
            {
                if ((lngs[node] > longitude != (lngs[altNode] > longitude))
                    && (latitude < ((lats[altNode] - lats[node])
                                       * (longitude - lngs[node])
                                       / (lngs[altNode] - lngs[node]))
                                       + lats[node]
                )
            )
                {
                    polygonContainsPoint = !polygonContainsPoint;
                }
            }
            lats.Clear();
            lngs.Clear();
            return polygonContainsPoint;
        }

        // Credits: http://codereview.stackexchange.com/a/108903
        public static bool IsPointInPolygon(Coordinate point, List<Coordinate> polygon)
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
                inside ^= ((endY > pointY) ^ (startY > pointY)) // ? pointY inside [startY;endY] segment ?
                          && // if so, test if it is under the segment
                          (pointX - endX < (pointY - endY) * (startX - endX) / (startY - endY));
            }
            return inside;
        }
    }
}