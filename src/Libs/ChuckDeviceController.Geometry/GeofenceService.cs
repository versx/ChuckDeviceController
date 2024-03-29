﻿namespace ChuckDeviceController.Geometry;

using System.Linq;

using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Geometry.Models.Abstractions;

public static class GeofenceService
{
    public static bool InMultiPolygon(IReadOnlyList<IMultiPolygon> multiPolygons, ICoordinate point)
    {
        var result = InMultiPolygon(multiPolygons, point.Latitude, point.Longitude);
        return result;
    }

    public static bool InMultiPolygon(IReadOnlyList<IMultiPolygon> multiPolygons, double latitude, double longitude)
    {
        var result = multiPolygons.Any(multiPolygon => InPolygon(multiPolygon, latitude, longitude));
        return result;
    }

    public static bool InPolygon(IMultiPolygon multiPolygon, Coordinate point)
    {
        var result = InPolygon(multiPolygon, point.Latitude, point.Longitude);
        return result;
    }

    public static bool InPolygon(IMultiPolygon multiPolygon, double latitude, double longitude)
    {
        var numOfPoints = multiPolygon.Count;
        var lats = multiPolygon.Select(coord => coord[0]).ToList();
        var lngs = multiPolygon.Select(coord => coord[1]).ToList();
        var polygonContainsPoint = false;
        for (int node = 0, altNode = (numOfPoints - 1); node < numOfPoints; altNode = node++)
        {
            if ((lngs[node] > longitude != (lngs[altNode] > longitude))
                && (latitude < ((lats[altNode] - lats[node])
                                   * (longitude - lngs[node])
                                   / (lngs[altNode] - lngs[node]))
                                   + lats[node]
            ))
            {
                polygonContainsPoint = !polygonContainsPoint;
            }
        }
        lats.Clear();
        lngs.Clear();
        return polygonContainsPoint;
    }

    // Credits: http://codereview.stackexchange.com/a/108903
    public static bool IsPointInPolygon<T>(ICoordinate point, IReadOnlyList<T> polygon)
        where T : ICoordinate
    {
        int polygonLength = polygon.Count, i = 0;
        var inside = false;
        // x, y for tested point.
        double pointX = point.Longitude, pointY = point.Latitude;
        // start / end point for the current polygon segment.
        double startX, startY, endX, endY;
        var endPoint = polygon[polygonLength - 1];
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

    public static bool IsPointInPolygon(Coordinate point, IReadOnlyList<IReadOnlyList<Coordinate>>? multiPolygons)
    {
        if (!(multiPolygons?.Any() ?? false))
            return true;

        foreach (var multiPolygon in multiPolygons!)
        {
            if (IsPointInPolygon(point, multiPolygon))
            {
                return true;
            }
        }
        return false;
    }
}