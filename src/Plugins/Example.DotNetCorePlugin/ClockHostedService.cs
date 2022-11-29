namespace Example.DotNetCorePlugin
{
    using System.Timers;

    using Microsoft.Extensions.Hosting;

    using ChuckDeviceController.Plugin;

    public class ClockHostedService : IHostedService
    {
        private const uint ClockUpdateInterval = 1 * 1000;

        private readonly Timer _timer;
        private readonly IUiHost _uiHost;

        public ClockHostedService(IUiHost uiHost)
        {
            _uiHost = uiHost;
            _timer = new Timer(ClockUpdateInterval);
            _timer.Elapsed += (sender, e) => OnClockTimerElapsed();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _uiHost.AddDashboardStatisticAsync(new DashboardStatsItem("Current Time", DateTime.Now.ToLongTimeString()));

            if (!_timer.Enabled)
            {
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
            await Task.CompletedTask;
        }

        private void OnClockTimerElapsed()
        {
            var time = DateTime.Now.ToLongTimeString();
            //Console.WriteLine($"Time: {time}");
            _uiHost.UpdateDashboardStatisticAsync(new DashboardStatsItem("Current Time", time));
        }
    }
}