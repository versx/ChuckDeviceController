namespace ChuckDeviceConfigurator.Utilities
{
    using ChuckDeviceController.Geometry.Models;

    public static class Utils
    {
        public static string FormatAssignmentTime(uint timeS)
        {
            var times = TimeSpan.FromSeconds(timeS);
            return timeS == 0
                ? "On Complete"
                : $"{times.Hours:00}:{times.Minutes:00}:{times.Seconds:00}";
        }

        public static double BenchmarkAction(Action action, ushort precision = 4)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Start();

            var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, precision);
            Console.WriteLine($"Benchmark took {totalSeconds}s");
            return totalSeconds;
        }

        public static int CompareCoordinates(Coordinate coord1, Coordinate coord2)
        {
            var d1 = Math.Pow(coord1.Latitude, 2) + Math.Pow(coord1.Longitude, 2);
            var d2 = Math.Pow(coord2.Latitude, 2) + Math.Pow(coord2.Longitude, 2);
            return d1.CompareTo(d2);
        }
    }
}