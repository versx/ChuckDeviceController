namespace ChuckDeviceController.HostedServices
{
    public abstract class TimedHostedService : BackgroundService, IDisposable
    {
        private const uint DefaultIntervalS = 3; // 3 seconds

        #region Variables

        private readonly ILogger _logger;
        private readonly CancellationTokenSource _stoppingCts = new();
        private readonly System.Timers.Timer _timer;
        private Task? _executingTask;

        #endregion

        #region Constructor

        public TimedHostedService(
            ILogger logger,
            uint timerIntervalS = DefaultIntervalS
            )
        {
            _logger = logger;
            _timer = new System.Timers.Timer(timerIntervalS * 1000);
            _timer.Elapsed += (sender, e) => InternalExecuteTask(null);
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
            if (!_timer.Enabled)
            {
                _timer.Start();
            }

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");
            if (_timer.Enabled)
            {
                _timer.Stop();
            }

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
            //_timer?.Change(TimeSpan.FromSeconds(TimerIntervalS), TimeSpan.FromMilliseconds(-1));
        }

        private void InternalExecuteTask(object? state)
        {
            //_timer?.Change(Timeout.Infinite, 0);
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