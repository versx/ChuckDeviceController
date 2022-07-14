namespace ChuckDeviceConfigurator.Services.Routing.Utilities
{
    using ChuckDeviceController.Geometry.Models;

    public static class RouteOptimizeUtil
    {
        public static List<Coordinate> Optimize(List<Coordinate> coords)
        {
            var start = coords.FirstOrDefault();
            var route = Optimize(coords, start.Latitude, start.Longitude);
            return route;
        }

        public static List<Coordinate> Optimize(List<Coordinate> coords, double lat, double lng)
        {
            var optimizedRoute = new List<Coordinate>(coords);
            // NN
            var nn = FindNn(optimizedRoute, lat, lng);
            optimizedRoute.Remove(nn);
            optimizedRoute.Insert(0, nn);
            for (var i = 1; i < coords.Count; i++)
            {
                nn = FindNn(optimizedRoute.Skip(i), nn.Latitude, nn.Longitude);
                optimizedRoute.Remove(nn);
                optimizedRoute.Insert(i, nn);
            }
            // 2-Opt
            bool isOptimized;
            do
            {
                optimizedRoute = Optimize2Opt(optimizedRoute, out isOptimized);
            } while (isOptimized);
            return optimizedRoute;
        }

        private static List<Coordinate> Optimize2Opt(List<Coordinate> coords, out bool isOptimized)
        {
            var count = coords.Count;
            float bestGain = 0;
            var bestI = -1;
            var bestJ = -1;

            for (var ai = 0; ai < count; ai++)
            {
                for (var ci = 0; ci < count; ci++)
                {
                    var bi = (ai + 1) % count;
                    var di = (ci + 1) % count;

                    var a = coords[ai];
                    var b = coords[bi];
                    var c = coords[ci];
                    var d = coords[di];

                    var ab = GetDistance(a, b);
                    var cd = GetDistance(c, d);
                    var ac = GetDistance(a, c);
                    var bd = GetDistance(b, d);

                    if (ci != ai && ci != bi)
                    {
                        var gain = ab + cd - (ac + bd);
                        if (gain > bestGain)
                        {
                            bestGain = gain;
                            bestI = bi;
                            bestJ = ci;
                        }
                    }
                }
            }

            if (bestI != -1)
            {
                List<Coordinate> optimizedRoute;
                if (bestI > bestJ)
                {
                    optimizedRoute = new List<Coordinate> { coords[0] };
                    optimizedRoute.AddRange(coords.Skip(bestI));
                    optimizedRoute.Reverse(1, count - bestI);
                    optimizedRoute.AddRange(coords.GetRange(bestJ + 1, bestI - bestJ - 1));
                    optimizedRoute.AddRange(coords.GetRange(1, bestJ));
                    optimizedRoute.Reverse(count - bestJ, bestJ);
                }
                else if (bestI == 0)
                {
                    optimizedRoute = new List<Coordinate>(coords);
                    optimizedRoute.Reverse(bestJ + 1, count - bestJ - 1);
                }
                else
                {
                    optimizedRoute = new List<Coordinate>(coords);
                    optimizedRoute.Reverse(bestI, bestJ - bestI + 1);
                }

                isOptimized = true;
                return optimizedRoute;
            }
            isOptimized = false;
            return coords;
        }

        private static Coordinate FindNn(IEnumerable<Coordinate> coords, double cLatitude, double cLongitude)
        {
            return coords.OrderBy(coord => GetDistance(cLatitude, cLongitude, coord.Latitude, coord.Longitude))
                         .FirstOrDefault();
        }

        private static float GetDistance(Coordinate coord1, Coordinate coord2)
        {
            return GetDistance(coord1.Latitude, coord1.Longitude, coord2.Latitude, coord2.Longitude);
        }

        private static float GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371e3;
            lat1 = toRad(lat1);
            lat2 = toRad(lat2);
            var dLng = toRad(lng2 - lng1);

            return (float)(Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(dLng)) * R);
        }

        static float toRad(double x) => (float)(x * (Math.PI / 180));
    }
}