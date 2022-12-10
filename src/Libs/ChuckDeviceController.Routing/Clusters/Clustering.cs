namespace ChuckDeviceController.Routing.Clusters
{
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models.Contracts;
    using ChuckDeviceController.Routing.Utilities;

    /// <summary>
    /// Credits: https://github.com/Kneckter/SpawnpointClusterTool/blob/master/cluster.py
    /// </summary>
    public class Clustering
    {
        private readonly List<List<CoordinateCluster>> _maxClustersList = new();
        private readonly List<ICoordinate> _points;
        private readonly double _radius;
        private readonly ushort _minPointsInCluster;

        public Clustering(
            List<ICoordinate> points,
            double radius = Strings.DefaultRadiusM,
            ushort minPointsInCluster = 1,
            List<List<ICoordinate>>? maxClusterList = null)
        {
            _points = points;
            _radius = radius;
            _minPointsInCluster = minPointsInCluster;

            if (maxClusterList?.Any() ?? false)
            {
                foreach (var cluster in maxClusterList)
                {
                    if (!cluster.Any())
                        continue;

                    _maxClustersList.Add(new()
                    {
                        new CoordinateCluster
                        {
                            Type = CoordinateType.Point,
                            Coordinate = cluster[0],
                        }
                    });
                }
            }
        }

        public List<ICoordinate> Calculate()
        {
            var results = CalculateClusters(_points, _radius, _minPointsInCluster);
            var coords = new List<ICoordinate>();
            foreach (var result in results)
            {
                var index = result.FindIndex(x => x.Type == CoordinateType.Cluster);
                var point = index > -1
                    ? result[index].Coordinate
                    : result[0].Coordinate;
                coords.Add(point);
            }
            return coords;
        }

        private List<List<CoordinateCluster>> CalculateClusters(List<ICoordinate> points, double radius, ushort minPointsInCluster)
        {
            var done = false;
            var start = DateTime.UtcNow.ToTotalSeconds();
            var workingPoints = new List<ICoordinate>(points);
            var workingClusterList = new List<List<CoordinateCluster>>(_maxClustersList);

            if (workingClusterList.Any())
            {
                var i = 0;
                while (i < workingPoints.Count)
                {
                    foreach (var cluster in workingClusterList)
                    {
                        var dist = GeoUtils.GetDistanceFromPoint(cluster[0].Coordinate, workingPoints[i]);
                        if (dist <= radius)
                        {
                            workingPoints.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                    i++;
                }
            }

            foreach (var point in workingPoints)
            {
                GetMpPoints(point);
            }

            foreach (var cluster in workingClusterList)
            {
                RemoveSmallClusters(cluster);
            }

            workingClusterList = new List<List<CoordinateCluster>>(_maxClustersList);
            while (workingClusterList.Count > 0 && !done)
            {
                var longestList = workingClusterList.MaxBy(x => x.Count)!; // MaxBy((x, y) => x.Count < y.Count)
                var hasClustersLeft = longestList.Count - 1 > minPointsInCluster && longestList.Count - 1 > 0;
                if (!hasClustersLeft)
                {
                    done = true;
                }

                workingClusterList.Remove(longestList);
                foreach (var item in longestList)
                {
                    if (item.Type == CoordinateType.Cluster)
                    {
                        _maxClustersList.Add(new()
                        {
                            new CoordinateCluster(CoordinateType.Cluster, item.Coordinate),
                        });
                    }
                    else
                    {
                        workingClusterList = RemoveLongestList(workingClusterList, item, minPointsInCluster);
                    }
                }
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            Console.WriteLine($"Clustering -- total time: {now - start}s for {points.Count:N0} points to {_maxClustersList.Count:N0} clusters");

            return _maxClustersList;
        }

        private static List<List<CoordinateCluster>> RemoveLongestList(List<List<CoordinateCluster>> clusterList, CoordinateCluster item, ushort minPointsInCluster)
        {
            var tmpClustersList = new List<List<CoordinateCluster>>(clusterList);
            for (var i = 0; i < tmpClustersList.Count; i++)
            {
                var cluster = tmpClustersList[i];
                if (cluster.Count - 1 < minPointsInCluster)
                {
                    tmpClustersList.Remove(cluster);
                    continue;
                }

                for (var j = 0; j < cluster.Count; j++)
                {
                    var coord = cluster[j];
                    if (coord.Type != CoordinateType.Point && item != coord)
                        continue;

                    var index = tmpClustersList.IndexOf(cluster);
                    if (index > -1)
                    {
                        cluster.Remove(coord);
                        break;
                    }
                }
            }
            return tmpClustersList;
        }

        private void RemoveSmallClusters(List<CoordinateCluster> cluster)
        {
            if (cluster.Count - 1 < _minPointsInCluster)
            {
                _maxClustersList.Remove(cluster);
            }
        }

        private void GetMpPoints(ICoordinate point)
        {
            var i = 0;
            var points = new List<CoordinateCluster>();

            while (i < _points.Count)
            {
                var dist = GeoUtils.GetDistanceFromPoint(_points[i], point);
                if (dist <= _radius)
                {
                    points.Add(new CoordinateCluster
                    {
                        Type = CoordinateType.Point,
                        Coordinate = _points[i],
                    });
                }
                if (dist == 0.0)
                {
                    points.Add(new CoordinateCluster
                    {
                        Type = CoordinateType.Cluster,
                        Coordinate = _points[i],
                    });
                }

                i++;
            }

            _maxClustersList.Add(points);
        }
    }
}