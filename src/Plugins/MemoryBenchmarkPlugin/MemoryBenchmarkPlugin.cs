namespace MemoryBenchmarkPlugin;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using ChuckDeviceController.Common;
using ChuckDeviceController.Plugin;

[PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
[StaticFilesLocation(StaticFilesLocation.Resources, StaticFilesLocation.External)]
public class MemoryBenchmarkPlugin : IPlugin
{
    //public const string MemoryBenchmarkRoleName = "MemoryBenchmark";
    //public const string MemoryBenchmarkRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{MemoryBenchmarkRoleName}";

    #region Variables

    private readonly IUiHost _uiHost;
    private readonly IAuthorizeHost _authHost;

    #endregion

    #region Metadata Properties

    public string Name => "MemoryBenchmarkPlugin";

    public string Description => "";

    public string Author => "versx";

    public Version Version => new(1, 0, 0);

    #endregion

    #region Constructor

    public MemoryBenchmarkPlugin(IUiHost uiHost, IAuthorizeHost authHost)
    {
        _uiHost = uiHost;
        _authHost = authHost;
    }

    #endregion

    #region ASP.NET Methods

    public void Configure(WebApplication appBuilder)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
    {
    }

    #endregion

    #region Plugin Methods

    public async void OnLoad()
    {
        var sidebarItem = new SidebarItem
        {
            Text = "Benchmarks",
            DisplayIndex = 1,
            Icon = "fa-solid fa-fw fa-clock",
            IsDropdown = true,
            DropdownItems = new List<SidebarItem>
            {
                new SidebarItem
                {
                    Text = "Memory",
                    ControllerName = "Memory",
                    ActionName = "Index",
                    Icon = "fa-solid fa-fw fa-memory",
                },
            },
        };
        await _uiHost.AddSidebarItemAsync(sidebarItem);

        //await _authHost.RegisterRole(MemoryBenchmarkRoleName);
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
    }

    #endregion
}