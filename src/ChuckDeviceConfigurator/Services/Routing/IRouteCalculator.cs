namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    public interface IRouteCalculator
    {
        void AddCoordinate(Coordinate coordinate);

        Queue<Coordinate> CalculateShortestRoute(Coordinate start);
    }
}