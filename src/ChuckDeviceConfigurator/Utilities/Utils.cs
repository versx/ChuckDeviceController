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

        // Credits: https://jasonwatmore.com/post/2018/10/17/c-pure-pagination-logic-in-c-aspnet
        public static List<int> GetNextPages(int page, int maxPages)
        {
            int startPage, endPage, maxMiddlePage = 5;
            if (maxPages <= maxMiddlePage)
            {
                // Total pages less than max so show all pages
                startPage = 1;
                endPage = maxPages;
            }
            else
            {
                var maxPagesBeforeCurrentPage = (int)Math.Floor((decimal)maxMiddlePage / (decimal)2);
                var maxPagesAfterCurrentPage = (int)Math.Ceiling((decimal)maxMiddlePage / (decimal)2) - 1;
                if (page <= maxPagesBeforeCurrentPage)
                {
                    // Current page near the start
                    startPage = 1;
                    endPage = maxMiddlePage;
                }
                else if (page + maxPagesAfterCurrentPage >= maxPages)
                {
                    // Current page near the end
                    startPage = maxPages - maxMiddlePage + 1;
                    endPage = maxPages;
                }
                else
                {
                    // Current page somewhere in the middle
                    startPage = page - maxPagesBeforeCurrentPage;
                    endPage = page + maxPagesAfterCurrentPage;
                }
            }

            // Create an array of pages that can be looped over
            var result = Enumerable.Range(startPage, (endPage + 1) - startPage).ToList();
            return result;
        }
    }
}