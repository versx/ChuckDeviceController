namespace Chuck.Infrastructure.Utilities
{
    using System;
    using System.Diagnostics;

    using Chuck.Infrastructure.Extensions;

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
                ConsoleExt.WriteInfo($"[DataConsumer] {type} Count: {count:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }
    }
}