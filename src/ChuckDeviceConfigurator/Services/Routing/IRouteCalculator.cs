namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    public interface IRouteCalculator
    {
        bool ClearCoordinatesAfterOptimization { get; set; }

        IReadOnlyList<Coordinate> Coordinates { get; }


        void AddCoordinate(Coordinate coordinate);

        void AddCoordinates(List<Coordinate> coordinates);

        void ClearCoordinates();

        Queue<Coordinate> CalculateShortestRoute();
    }
}