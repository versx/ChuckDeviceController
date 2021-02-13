namespace ChuckDeviceController.Geofence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ChuckDeviceController.Geofence.Models;

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
                    && (latitude < (lats[altNode] - lats[node])
                                       * (longitude - lngs[node])
                                       / (lngs[altNode] - lngs[node])
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
    }
}