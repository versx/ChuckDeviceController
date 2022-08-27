namespace ChuckDeviceController.Plugin
{
    using System.Diagnostics;

    /// <summary>
    /// Used to contain timing data for requests.
    /// 
    /// This stores the number of requests and total time in milleseconds
    /// serving the requests.
    /// </summary>
    public sealed class RequestTimings
    {
        #region Variables

        private readonly object _lock = new();
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
        /// Gets the total number of milliseconds used for the request that was slowest.
        /// </summary>
        public decimal Slowest => _slowest == decimal.MinValue ? 0 : CalculateTiming(_slowest, DecimalPlaces);

        /// <summary>
        /// Gets the total number of milliseconds used for the request that was quickest.
        /// </summary>
        public decimal Fastest => _fastest == decimal.MaxValue ? 0 : CalculateTiming(_fastest, DecimalPlaces);

        /// <summary>
        /// Gets the average number of milliseconds per request.
        /// </summary>
        public decimal Average => CalculateTiming(_average, DecimalPlaces);

        /// <summary>
        /// Gets the calculated trimmed average by removing the highest and lowest scores before averaging
        /// </summary>
        public decimal TrimmedAverage
        {
            get
            {
                if (_total == 0 || Requests < 3)
                    return 0;

                return CalculateTiming((_total - (_fastest + _slowest)) / (Requests - 2), DecimalPlaces);
            }
        }

        /// <summary>
        /// Gets the total number of requests.
        /// </summary>
        public decimal Total => CalculateTiming(_total, DecimalPlaces);

        /// <summary>
        /// Gets the number of decimal places the results should be rounded to, default is 5
        /// </summary>
        public byte DecimalPlaces { get; set; }

        /// <summary>
        /// Gets a value determining whether the timings have been cloned or not.
        /// </summary>
        public bool IsCloned { get; }

        /// <summary>
        /// Gets a value determining whether to log request times or not.
        /// </summary>
        public bool LogTimings { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public RequestTimings(bool logTimings = false)
        {
            _fastest = decimal.MaxValue;
            _slowest = decimal.MinValue;
            _average = 0;
            _total = 0;

            DecimalPlaces = 4;
            IsCloned = false;
            LogTimings = logTimings;
        }

        private RequestTimings(decimal fastest, decimal slowest, decimal average, decimal total, uint requests, byte decimalPlaces, bool logTimings = false)
        {
            _fastest = fastest;
            _slowest = slowest;
            _average = average;
            _total = total;

            Requests = requests;
            DecimalPlaces = decimalPlaces;
            IsCloned = true;
            LogTimings = logTimings;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Increments the total milliseconds
        /// </summary>
        /// <param name="stopWatch"></param>
        public void Increment(Stopwatch stopWatch)
        {
            if (stopWatch == null)
            {
                throw new ArgumentNullException(nameof(stopWatch));
            }

            Increment(stopWatch.ElapsedTicks);
        }

        /// <summary>
        /// Increments the total ticks
        /// </summary>
        /// <param name="totalTicks">Total number of ticks to increment by.</param>
        public void Increment(long totalTicks)
        {
            lock (_lock)
            {
                Requests++;

                if (totalTicks < _fastest)
                    _fastest = totalTicks;

                if (totalTicks > _slowest)
                    _slowest = totalTicks;

                _total += totalTicks;

                if (_total > 0)
                    _average = _total / Requests;
            }

            if (LogTimings)
            {
                LogRequestTime();
            }
        }

        /// <summary>
        /// Clones an instance of a Timings class
        /// </summary>
        /// <returns>Timings</returns>
        public RequestTimings Clone()
        {
            lock (_lock)
            {
                return new RequestTimings(_fastest, _slowest, _average, _total, Requests, DecimalPlaces, LogTimings);
            }
        }

        #endregion

        private void LogRequestTime()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Requests: {Requests}, ");
            sb.Append($"Slowest: {Slowest}ms, ");
            sb.Append($"Fastest: {Fastest}ms, ");
            sb.Append($"Average: {Average}ms, ");
            sb.Append($"TrimmedAverage: {TrimmedAverage}ms, ");
            sb.Append($"Total: {Total}, ");
            sb.Append($"DecimalPlaces: {DecimalPlaces}, ");
            sb.Append($"IsCloned: {IsCloned}");
            Console.WriteLine(sb.ToString());
        }

        private static decimal CalculateTiming(decimal value, ushort decimalPlaces = 5)
        {
            var result = Math.Round(value / TimeSpan.TicksPerMillisecond, decimalPlaces, MidpointRounding.AwayFromZero);
            return result;
        }
    }
}