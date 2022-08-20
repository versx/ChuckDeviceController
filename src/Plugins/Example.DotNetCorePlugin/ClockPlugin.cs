namespace Example.DotNetCorePlugin
{
    using System;
    using System.Timers;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.Plugin.Services;

    /// <summary>
    /// Basic plugin example to demostrate adding and updating
    /// the current time on the dashboard page of the host
    /// application under the statistics section.
    /// </summary>
    public class ClockPlugin : IPlugin
    {
        private readonly Timer _timer;
        private const uint ClockUpdateInterval = 1 * 1000;

        #region Metadata Properties

        public string Name => "ClockPlugin";

        public string Description => "Displays and updates the current time.";

        public string Author => "versx";

        public Version Version => new(1, 0, 0);

        #endregion

        public IUiHost UiHost { get; }

        public ClockPlugin(IUiHost uiHost)
        {
            _timer = new Timer(ClockUpdateInterval);
            _timer.Elapsed += (sender, e) => OnClockTimerElapsed();

            UiHost = uiHost;
        }

        #region Implementation Methods

        public void Configure(WebApplication appBuilder)
        {
            // Unused with .NET Core plugins, only used with ASP.NET Core plugins
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Unused with .NET Core plugins, only used with ASP.NET Core plugins
        }

        public void OnLoad()
        {
            // Call any UI additions to add to the host here
            UiHost.AddDashboardStatisticAsync(new DashboardStatsItem("Current Time", DateTime.Now.ToLongTimeString()));

            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        public void OnReload()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        public void OnRemove()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        public void OnStop()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        public void OnStateChanged(PluginState state)
        {
            switch (state)
            {
                case PluginState.Unset:
                case PluginState.Running:
                case PluginState.Stopped:
                case PluginState.Disabled:
                case PluginState.Removed:
                case PluginState.Error:
                    break;
            }
        }

        #endregion

        private void OnClockTimerElapsed()
        {
            var time = DateTime.Now.ToLongTimeString();
            Console.WriteLine($"Time: {time}");
            UiHost.UpdateDashboardStatisticAsync(new DashboardStatsItem("Current Time", time));
        }
    }
}