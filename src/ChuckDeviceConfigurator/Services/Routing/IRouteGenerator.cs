﻿namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    public interface IRouteGenerator
    {
        List<Coordinate> GenerateRoute(RouteGeneratorOptions options);

        List<Coordinate> GenerateBootstrapRoute(List<MultiPolygon> multiPolygons, double circleSize = 70);

        List<Coordinate> GenerateBootstrapRoute(MultiPolygon multiPolygon, double circleSize = 70);

        List<Coordinate> GenerateRandomRoute(List<MultiPolygon> multiPolygon, uint maxPoints = 500, double circleSize = 70);

        List<Coordinate> GenerateRandomRoute(MultiPolygon multiPolygon, uint maxPoints = 500, double circleSize = 70);

        List<Coordinate> GenerateOptimizedRoute(List<MultiPolygon> multiPolygons, double circleSize = 70);

        List<Coordinate> GenerateOptimizedRoute(MultiPolygon multiPolygon, double circleSize = 70);
    }
}