namespace RequestBenchmarkPlugin.Utilities
{
    using System.Diagnostics;

    /// <summary>
    /// Stopwatch timer is used to automatically collect and generate request timing data.
    /// </summary>
    public readonly struct StopwatchTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly RequestBenchmark _benchmark;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="benchmark">Benchmark class used to measure request timing.</param>
        public StopwatchTimer(RequestBenchmark benchmark)
        {
            _benchmark = benchmark ?? throw new ArgumentNullException(nameof(benchmark));
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        /// <summary>
        /// Initializes an instance of the StopwatchTimer class used to collect request timings.
        /// </summary>
        /// <param name="benchmark">Benchmark class used to measure request timing.</param>
        public static StopwatchTimer Initialize(RequestBenchmark benchmark) => new(benchmark);

        /// <summary>
        /// Dispose method which ensures resources are disposed of and timing data is recorded.
        /// </summary>
        public void Dispose()
        {
            _stopwatch.Stop();
            _benchmark.Increment(_stopwatch);
        }
    }
}