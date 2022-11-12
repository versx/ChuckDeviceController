namespace RequestBenchmarkPlugin.Utilities
{
    using System.Diagnostics;

    /// <summary>
    /// Used to measure benchmark timing information for requests.
    /// 
    /// This stores the number of requests and total time in milliseconds
    /// serving the requests.
    /// </summary>
    public sealed class RequestBenchmark
    {
        #region Variables

        private decimal _fastest;
        private decimal _slowest;
        private decimal _average;
        private decimal _total;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the total number of requests made.
        /// </summary>
        public uint Requests { get; private set; }

        /// <summary>
        /// Gets the total number of milliseconds used for the request that
        /// was the slowest.
        /// </summary>
        public decimal Slowest => _slowest == decimal.MinValue ? 0 : CalculateTime(_slowest, DecimalPlaces);

        /// <summary>
        /// Gets the total number of milliseconds used for the request that
        /// was the quickest.
        /// </summary>
        public decimal Fastest => _fastest == decimal.MaxValue ? 0 : CalculateTime(_fastest, DecimalPlaces);

        /// <summary>
        /// Gets the average number of milliseconds per request.
        /// </summary>
        public decimal Average => CalculateTime(_average, DecimalPlaces);

        /// <summary>
        /// Gets the total number of requests.
        /// </summary>
        public decimal Total => CalculateTime(_total, DecimalPlaces);

        /// <summary>
        /// Gets the number of decimal places the results should be rounded
        /// to, default is 4
        /// </summary>
        public byte DecimalPlaces { get; set; } = 4;

        /// <summary>
        /// Gets a value determining whether the timings have been cloned
        /// or not.
        /// </summary>
        public bool IsCloned { get; }

        /// <summary>
        /// Gets or sets the route of the request.
        /// </summary>
        public string Route { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public RequestBenchmark(string route)
        {
            _fastest = decimal.MaxValue;
            _slowest = decimal.MinValue;
            _average = 0;
            _total = 0;

            Route = route;
            DecimalPlaces = 4;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Increments the total milliseconds
        /// </summary>
        /// <param name="stopwatch"></param>
        public void Increment(Stopwatch stopwatch)
        {
            if (stopwatch == null)
            {
                throw new ArgumentNullException(nameof(stopwatch));
            }

            Increment(stopwatch.ElapsedTicks);
        }

        /// <summary>
        /// Increments the total ticks
        /// </summary>
        /// <param name="totalTicks">Total number of ticks to increment by.</param>
        public void Increment(long totalTicks)
        {
            Requests++;

            if (totalTicks < _fastest)
            {
                _fastest = totalTicks;
            }

            if (totalTicks > _slowest)
            {
                _slowest = totalTicks;
            }

            _total += totalTicks;

            if (_total > 0)
            {
                _average = _total / Requests;
            }
        }

        #endregion

        #region Private Methods

        private static decimal CalculateTime(decimal value, ushort decimalPlaces = 4)
        {
            var ticks = value / TimeSpan.TicksPerMillisecond;
            var result = Math.Round(ticks, decimalPlaces, MidpointRounding.AwayFromZero);
            return result;
        }

        #endregion
    }
}