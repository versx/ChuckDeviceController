namespace Chuck.Common.Utilities
{
    using System;
    using System.Diagnostics;

    public static class BenchmarkUtils
    {
        public static void BenchmarkMethod(Func<int> method, string type)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var count = method();
            stopwatch.Stop();
            if (count > 0)
            {
                Console.WriteLine($"{type} Count: {count:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }
    }
}