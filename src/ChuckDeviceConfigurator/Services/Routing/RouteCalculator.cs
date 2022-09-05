namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceController.Common.Geometry;

    public class RouteCalculator : IRouteCalculator
    {
        private const ushort DefaultCircleSize = Strings.DefaultCircleSize;

        private readonly List<ICoordinate> _coordinates;

        #region Properties

        /// <summary>
        /// Gets or sets a value determining whether to clear the list of
        /// coordinates after optimizing the route. Default value is <c>true</c>
        /// </summary>
        public bool ClearCoordinatesAfterOptimization { get; set; } = true;

        /// <summary>
        /// Gets a read only list of <seealso cref="ICoordinate"/>.
        /// </summary>
        public IReadOnlyList<ICoordinate> Coordinates => _coordinates;

        #endregion

        #region Constructors

        public RouteCalculator()
            : this(new List<ICoordinate>())
        {
        }

        public RouteCalculator(List<ICoordinate> coordinates)
        {
            _coordinates = coordinates;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the provided coordinate to the coordinate list.
        /// </summary>
        /// <param name="coordinate">Coordinate to add.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddCoordinate(ICoordinate coordinate)
        {
            if (coordinate == null)
            {
                throw new ArgumentNullException(nameof(coordinate));
            }
            _coordinates.Add(coordinate);
        }

        /// <summary>
        /// Adds a list of coordinates to the coordinate list.
        /// </summary>
        /// <param name="coordinates">List of coordinates to add.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddCoordinates(List<ICoordinate> coordinates)
        {
            if ((coordinates?.Count ?? 0) == 0)
            {
                throw new ArgumentNullException(nameof(coordinates));
            }
            foreach (var coordinate in coordinates!)
            {
                AddCoordinate(coordinate);
            }
        }

        /// <summary>
        /// Clears all coordinates in the coordinate list.
        /// </summary>
        public void ClearCoordinates()
        {
            _coordinates.Clear();
        }

        /// <summary>
        /// Calculates the shortest possible route.
        /// </summary>
        /// <returns>Returns a queue of the shortest route.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Queue<ICoordinate> CalculateShortestRoute()
        {
            if (_coordinates.Count == 0)
            {
                throw new InvalidOperationException("No coordinates entered");
            }

            var sorted = _coordinates;
            sorted.Sort(Utils.CompareCoordinates);
            var ordered = OrderByDistance(sorted);

            if (ClearCoordinatesAfterOptimization)
            {
                ClearCoordinates();
            }

            var queue = new Queue<ICoordinate>(ordered);
            return queue;
        }

        #endregion

        #region Private Methods

        private static double GetDistance(ICoordinate coord1, ICoordinate coord2)
        {
            return Math.Sqrt(
                Math.Pow(coord2.Latitude - coord1.Latitude, 2) +
                Math.Pow(coord2.Longitude - coord1.Longitude, 2)
            );
        }

        private static double GetDistanceQuick(ICoordinate coord1, ICoordinate coord2)
        {
            var deltaX = Math.Abs(coord2.Latitude - coord1.Latitude);
            var deltaY = Math.Abs(coord2.Longitude - coord1.Longitude);
            var distance = deltaX > deltaY ? deltaX : deltaY;
            return distance;
        }

        private static List<ICoordinate> OrderByDistance(List<ICoordinate> coordinates, ushort circleSize = DefaultCircleSize)
        {
            var orderedList = new List<ICoordinate>();
            var coords = new List<ICoordinate>(coordinates);
            var currentPoint = coords[0];

            while (coords.Count > 1)
            {
                orderedList.Add(currentPoint);
                coords.RemoveAt(coords.IndexOf(currentPoint));
                var closestPointIndex = 0;
                var closestDistance = double.MaxValue;

                for (var i = 0; i < coords.Count; i++)
                {
                    var distanceQuick = GetDistanceQuick(currentPoint, coords[i]) + (circleSize / 2);
                    //var distanceQuick = currentPoint.DistanceTo(coordinates[i]) + (circleSize / 2);
                    if (distanceQuick > closestDistance)
                        continue;

                    var distance = GetDistance(currentPoint, coords[i]) + (circleSize / 2);
                    //var distance = currentPoint.DistanceTo(coordinates[i]) + (circleSize / 2);
                    if (distance < closestDistance)
                    {
                        closestPointIndex = i;
                        closestDistance = distance;
                    }
                }
                currentPoint = coords[closestPointIndex];
            }

            // Add the last point
            orderedList.Add(currentPoint);
            return orderedList;
        }

        #endregion
    }
}