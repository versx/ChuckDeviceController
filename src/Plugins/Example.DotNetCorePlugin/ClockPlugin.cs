﻿namespace Example.DotNetCorePlugin;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Plugin;

/// <summary>
///     Basic plugin example to demostrate adding and updating
///     the current time on the dashboard page of the host
///     application under the statistics section.
/// </summary>
[PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
public class ClockPlugin : IPlugin
{
    private readonly IUiHost _uiHost;

    #region Metadata Properties

    public string Name => "ClockPlugin";

    public string Description => "Displays and updates the current time.";

    public string Author => "versx";

    public Version Version => new(1, 0, 0);

    #endregion

    public ClockPlugin(IUiHost uiHost)
    {
        _uiHost = uiHost;
    }

    #region ASP.NET Core Configuration Callback Methods

    public void Configure(WebApplication appBuilder)
    {
        // Unused with .NET Core plugins, only used with ASP.NET Core plugins
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Unused with .NET Core plugins, only used with ASP.NET Core plugins
    }

    public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
    {
        // Unused with .NET Core plugins, only used with ASP.NET Core plugins
    }

    #endregion

    #region Implementation Methods

    public async void OnLoad()
    {
        // Call any UI additions to add to the host here
        var timeStat = new DashboardStatsItem(
            "Current Time",
            valueUpdater: new Func<string>(() => DateTime.Now.ToLongTimeString())
        );
        await _uiHost.AddDashboardStatisticAsync(timeStat);
    }

    public void OnReload()
    {
    }

    public void OnRemove()
    {
    }

    public void OnStop()
    {
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
}