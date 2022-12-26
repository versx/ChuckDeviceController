namespace PogoEventsPlugin;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Plugin;

using Configuration;
using Services;
using Services.Discord;

// TODO: Integrate with main application, allow setting active event to adjust IV lists and such

[PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
[StaticFilesLocation(StaticFilesLocation.Resources, StaticFilesLocation.External)]
public class PogoEventsPlugin : IPlugin
{
    public const string PogoEventsRoleName = "PogoEvents";
    public const string PogoEventsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{PogoEventsRoleName}";

    #region Variables

    private readonly IUiHost _uiHost;
    private readonly IAuthorizeHost _authHost;
    private readonly IConfigurationHost _configHost;
    private readonly IConfiguration _configuration;

    #endregion

    #region Metadata Properties

    public string Name => "PogoEventsPlugin";

    public string Description => "Retrieve and manage active Pokemon Go events.";

    public string Author => "versx";

    public Version Version => new(1, 0, 0);

    #endregion

    #region Constructor

    public PogoEventsPlugin(IUiHost uiHost, IAuthorizeHost authHost, IConfigurationHost configHost)
    {
        _uiHost = uiHost;
        _authHost = authHost;
        _configHost = configHost;
        _configuration = _configHost.GetConfiguration();
    }

    #endregion

    #region ASP.NET Methods

    public async void Configure(WebApplication appBuilder)
    {
        var eventDataService = appBuilder.Services.GetRequiredService<IPokemonEventDataService>();
        await eventDataService.StartAsync();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<DiscordConfig>(_configuration.GetSection("Discord"));

        services.AddSingleton<IPokemonEventDataService, PokemonEventDataService>();
        services.AddSingleton<IDiscordClientService, DiscordClientService>();
    }

    public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
    {
    }

    #endregion

    #region Plugin Methods

    public async void OnLoad()
    {
        var navbarHeader = new SidebarItem
        {
            Text = "In-Game Events",
            ControllerName = "Event",
            ActionName = "Index",
            DisplayIndex = 2,
            Icon = "fa-solid fa-fw fa-calendar-day",
        };
        await _uiHost.AddSidebarItemAsync(navbarHeader);

        await _authHost.RegisterRole(PogoEventsRoleName, 10);
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