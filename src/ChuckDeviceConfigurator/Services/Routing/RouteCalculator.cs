namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    public class RouteCalculator : IRouteCalculator
    {
        private const ushort DefaultCircleSize = 70;

        private readonly List<Coordinate> _coordinates;

        public RouteCalculator()
        {
            _coordinates = new List<Coordinate>();
        }

        public RouteCalculator(List<Coordinate> coordinates)
        {
            _coordinates = coordinates;
        }

        #region Public Methods

        public void AddCoordinate(Coordinate coordinate)
        {
            if (coordinate == null)
            {
                throw new ArgumentNullException(nameof(coordinate));
            }
            _coordinates.Add(coordinate);
        }

        public Queue<Coordinate> CalculateShortestRoute(Coordinate start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (_coordinates.Count == 0)
            {
                throw new InvalidOperationException("No coordinates entered");
            }

            //var route = new Queue<Coordinate>(_coordinates.Count);
            var sorted = _coordinates;
            sorted.Sort((a, b) =>
            {
                var d1 = Math.Pow(a.Latitude, 2) + Math.Pow(a.Longitude, 2);
                var d2 = Math.Pow(b.Latitude, 2) + Math.Pow(b.Longitude, 2);
                return d1.CompareTo(d2);
            });
            var ordered = OrderByDistance(sorted);
            var queue = new Queue<Coordinate>(ordered);
            return queue;
        }

        #endregion

        #region Private Methods

        private static double GetDistance(Coordinate coord1, Coordinate coord2)
        {
            var distance = Math.Sqrt
            (
                Math.Pow(coord2.Latitude - coord1.Latitude, 2) +
                Math.Pow(coord2.Longitude - coord1.Longitude, 2)
            );
            return distance;
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
                    if (distanceQuick > closestDistance)
                        continue;

                    var distance = GetDistance(currentPoint, coordinates[i]) + (circleSize / 2);
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