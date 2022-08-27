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
        /// Total number of requests made.
        /// </summary>
        public uint Requests { get; private set; }

        /// <summary>
        /// Indicates the total number of milliseconds used for the request that was slowest.
        /// </summary>
        public decimal Slowest
        {
            get
            {
                if (_slowest == decimal.MinValue)
                    return 0;

                return Math.Round(_slowest / TimeSpan.TicksPerMillisecond, DecimalPlaces, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Indicates the total number of milliseconds used for the request that was quickest.
        /// </summary>
        public decimal Fastest
        {
            get
            {
                if (_fastest == decimal.MaxValue)
                    return 0;

                return Math.Round(_fastest / TimeSpan.TicksPerMillisecond, DecimalPlaces, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Returns the average number of milliseconds per request.
        /// </summary>
        /// <value>decimal</value>
        public decimal Average
        {
            get
            {
                return Math.Round(_average / TimeSpan.TicksPerMillisecond, DecimalPlaces, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Calculates the trimmed average, by removing the highest and lowest scores before averaging
        /// </summary>
        public decimal TrimmedAverage
        {
            get
            {
                if (_total == 0 || Requests < 3)
                    return 0;

                return Math.Round((_total - (_fastest + _slowest)) / (Requests - 2) / TimeSpan.TicksPerMillisecond, DecimalPlaces, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Returns the total number of requests.
        /// </summary>
        /// <value>long</value>
        public decimal Total
        {
            get
            {
                return Math.Round(_total / TimeSpan.TicksPerMillisecond, DecimalPlaces, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Number of decimal places the results should be rounded to, default is 5
        /// </summary>
        /// <value>byte</value>
        public byte DecimalPlaces { get; set; }

        /// <summary>
        /// Indicates whether the Timings have been cloned or not
        /// </summary>
        /// <value>bool</value>
        public bool IsCloned { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public RequestTimings()
        {
            _fastest = decimal.MaxValue;
            _slowest = decimal.MinValue;
            _average = 0;
            _total = 0;
            DecimalPlaces = 4;
            IsCloned = false;
        }

        private RequestTimings(decimal fastest, decimal slowest, decimal average, decimal total, uint requests, byte decimalPlaces)
        {
            _fastest = fastest;
            _slowest = slowest;
            _average = average;
            _total = total;
            Requests = requests;
            DecimalPlaces = decimalPlaces;
            IsCloned = true;
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

        /// <summary>
        /// Clones an instance of a Timings class
        /// </summary>
        /// <returns>Timings</returns>
        public RequestTimings Clone()
        {
            lock (_lock)
            {
                return new RequestTimings(_fastest, _slowest, _average, _total, Requests, DecimalPlaces);
            }
        }

        #endregion
    }
}