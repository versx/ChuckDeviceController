namespace ChuckDeviceController.Services.Routes
{
    using ChuckDeviceController.Data.Entities;
    using System;
    using System.Collections.Generic;

    public class RouteCalculator
	{
		private readonly List<Coordinate> _destinations;

		public RouteCalculator(List<Coordinate> coordinates)
		{
			_destinations = coordinates;
		}

		public void AddDestination(Coordinate destination)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}
			_destinations.Add(destination);
		}

		public Queue<Coordinate> CalculateShortestRoute(Coordinate start)
		{
			if (start == null)
			{
				throw new ArgumentNullException(nameof(start));
			}
			if (_destinations.Count == 0)
			{
				throw new InvalidOperationException("No Destinations entered");
			}
            Queue<Coordinate> route = new Queue<Coordinate>(_destinations.Count);
            List<Coordinate> sorted = _destinations;
			sorted.Sort((a, b) =>
			{
				double d1 = Math.Pow(a.Latitude, 2) + Math.Pow(a.Longitude, 2);
				double d2 = Math.Pow(b.Latitude, 2) + Math.Pow(b.Longitude, 2);
				return d1.CompareTo(d2);
			});
			return new Queue<Coordinate>(
				OrderByDistance(sorted)
			);
		}

		private static double Distance(Coordinate p1, Coordinate p2)
		{
			return Math.Sqrt(
				Math.Pow(p2.Latitude - p1.Latitude, 2)
				+ Math.Pow(p2.Longitude - p1.Longitude, 2)
			);
		}
		private static double DistanceQuick(Coordinate p1, Coordinate p2)
		{
			// distance will this or less
			double deltaX = Math.Abs(p2.Latitude - p1.Latitude);
			double deltaY = Math.Abs(p2.Longitude - p1.Longitude);
			return deltaX > deltaY ? deltaX : deltaY;
		}
		private static List<Coordinate> OrderByDistance(List<Coordinate> points, ushort circleSize = 70)
		{
            List<Coordinate> pointsList = points;
            List<Coordinate> orderedList = new List<Coordinate>();
            Coordinate currentPoint = pointsList[0];
			while (pointsList.Count > 1)
			{
				orderedList.Add(currentPoint);
				pointsList.RemoveAt(pointsList.IndexOf(currentPoint));
                int closestPointIndex = 0;
                double closestDistance = double.MaxValue;
				for (int i = 0; i < pointsList.Count; i++)
				{
                    double distanceQuick = DistanceQuick(currentPoint, pointsList[i]) + (circleSize / 2);
					if (distanceQuick > closestDistance)
                    {
                        continue;
                    }

                    double distance = Distance(currentPoint, pointsList[i]) + (circleSize / 2);
					if (distance < closestDistance)
					{
						closestPointIndex = i;
						closestDistance = distance;
					}
				}
				currentPoint = pointsList[closestPointIndex];
			}
			// Add the last point.
			orderedList.Add(currentPoint);
			return orderedList;
		}
	}
}