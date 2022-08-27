namespace TestPlugin
{
    using System.Diagnostics;

    using ChuckDeviceController.Plugin;

    public sealed class TimingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly RequestTimings _timings = new();

        public TimingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (var stopwatchTimer = StopWatchTimer.Initialise(_timings))
            {
                Console.WriteLine($"Timing middleware");
            }
            await _next(context);
        }
    }

    /// <summary>
    /// Stopwatch Timer is used to automatically collect and generate timing data accurate to 1000th of a millisecond.
    /// </summary>
    public readonly struct StopWatchTimer : IDisposable
    {
        #region Variables

        private readonly Stopwatch _stopwatch;
        private readonly RequestTimings _timings;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timings">SharedPluginFeatures.Timings class used to contain timing data.</param>
        public StopWatchTimer(RequestTimings timings)
        {
            _timings = timings ?? throw new ArgumentNullException(nameof(timings));
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Initialises an instance of the StopWatchTimer class used to collect timings.
        /// </summary>
        /// <param name="timings">SharedPluginFeatures.Timings class used to contain timing data.</param>
        /// <returns>StopWatchTimer</returns>
        public static StopWatchTimer Initialise(RequestTimings timings) => new(timings);

        #endregion

        #region IDisposable Methods

        /// <summary>
        /// Dispose method which ensures resources are disposed of and timing data is recorded.
        /// </summary>
        public void Dispose()
        {
            _stopwatch.Stop();
            _timings.Increment(_stopwatch);
        }

        #endregion
    }
}
