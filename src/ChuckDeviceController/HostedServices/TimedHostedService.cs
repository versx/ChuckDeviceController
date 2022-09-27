namespace ChuckDeviceController.HostedServices
{
    public abstract class TimedHostedService : BackgroundService, IDisposable
    {
        private const uint DefaultIntervalMs = 3 * 1000; // 3 seconds

        #region Variables

        private static readonly ILogger _logger =
            new Logger<TimedHostedService>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly CancellationTokenSource _stoppingCts = new();
        private Timer? _timer;
        private Task? _executingTask;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the callback timer interval in milliseconds.
        /// </summary>
        public virtual uint TimerIntervalMs { get; private set; } = DefaultIntervalMs;

        #endregion

        #region Constructor

        public TimedHostedService()
        {
        }

        #endregion

        #region Impl Overrides

        /// <summary>
        /// Starts the callback timer of the hosted service.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");
            _timer = new Timer(
                InternalExecuteTask,
                null,
                TimeSpan.FromMilliseconds(TimerIntervalMs),
                TimeSpan.FromMilliseconds(-1)
            );

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);

            // Stop called without start
            if (_executingTask == null)
                return;

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(
                    _executingTask,
                    Task.Delay(Timeout.Infinite, cancellationToken)
                );
            }
        }

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task 
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
        /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
        protected abstract Task RunJobAsync(CancellationToken stoppingToken);

        #endregion

        #region Private Methods

        private async Task ExecuteTaskAsync(CancellationToken stoppingToken)
        {
            await RunJobAsync(stoppingToken);
            _timer?.Change(TimeSpan.FromMilliseconds(TimerIntervalMs), TimeSpan.FromMilliseconds(-1));
        }

        private void InternalExecuteTask(object? state)
        {
            _timer?.Change(Timeout.Infinite, 0);
            _executingTask = ExecuteTaskAsync(_stoppingCts.Token);
        }

        #endregion

        #region Dispose Implementation

        public override void Dispose()
        {
            _stoppingCts.Cancel();
            _timer?.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}