namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class RouteCalculator : IRouteCalculator
    {
        private const ushort DefaultCircleSize = 70;

        private readonly List<Coordinate> _coordinates;

        #region Constructors

        public RouteCalculator()
        {
            _coordinates = new List<Coordinate>();
        }

        public RouteCalculator(List<Coordinate> coordinates)
        {
            _coordinates = coordinates;
        }

        #endregion

        #region Public Methods

        public void AddCoordinate(Coordinate coordinate)
        {
            if (coordinate == null)
            {
                throw new ArgumentNullException(nameof(coordinate));
            }
            _coordinates.Add(coordinate);
        }

        public void AddCoordinates(List<Coordinate> coordinates)
        {
            if (coordinates?.Count == 0)
            {
                throw new ArgumentNullException(nameof(coordinates));
            }
            foreach (var coordinate in coordinates)
            {
                AddCoordinate(coordinate);
            }
        }

        public void ClearCoordinates()
        {
            _coordinates.Clear();
        }

        public Queue<Coordinate> CalculateShortestRoute()
        {
            if (_coordinates.Count == 0)
            {
                throw new InvalidOperationException("No coordinates entered");
            }

            var sorted = _coordinates;
            sorted.Sort(Utils.CompareCoordinates);
            var ordered = OrderByDistance(sorted);
            var queue = new Queue<Coordinate>(ordered);
            return queue;
        }

        #endregion

        #region Private Methods

        private static double GetDistance(Coordinate coord1, Coordinate coord2)
        {
            return Math.Sqrt(
                Math.Pow(coord2.Latitude - coord1.Latitude, 2) +
                Math.Pow(coord2.Longitude - coord1.Longitude, 2)
            );
        }

        private static double GetDistanceQuick(Coordinate coord1, Coordinate coord2)
        {
            var deltaX = Math.Abs(coord2.Latitude - coord1.Latitude);
            var deltaY = Math.Abs(coord2.Longitude - coord1.Longitude);
            var distance = deltaX > deltaY ? deltaX : deltaY;
            return distance;
        }

        private static List<Coordinate> OrderByDistance(List<Coordinate> coordinates, ushort circleSize = DefaultCircleSize)
        {
            var orderedList = new List<Coordinate>();
            var currentPoint = coordinates[0];

            while (coordinates.Count > 1)
            {
                orderedList.Add(currentPoint);
                coordinates.RemoveAt(coordinates.IndexOf(currentPoint));
                var closestPointIndex = 0;
                var closestDistance = double.MaxValue;

                for (var i = 0; i < coordinates.Count; i++)
                {
                    var distanceQuick = GetDistanceQuick(currentPoint, coordinates[i]) + (circleSize / 2);
                    //var distanceQuick = currentPoint.DistanceTo(coordinates[i]) + (circleSize / 2);
                    if (distanceQuick > closestDistance)
                        continue;

                    var distance = GetDistance(currentPoint, coordinates[i]) + (circleSize / 2);
                    //var distance = currentPoint.DistanceTo(coordinates[i]) + (circleSize / 2);
                    if (distance < closestDistance)
                    {
                        closestPointIndex = i;
                        closestDistance = distance;
                    }
                }
                currentPoint = coordinates[closestPointIndex];
            }

            // Add the last point
            orderedList.Add(currentPoint);
            return orderedList;
        }

        #endregion
    }
}