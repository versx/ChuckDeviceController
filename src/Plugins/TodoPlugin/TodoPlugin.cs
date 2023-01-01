namespace TodoPlugin;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Data.Contexts;

using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Plugin;

[PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
[StaticFilesLocation(StaticFilesLocation.Resources, StaticFilesLocation.External)]
public class TodoPlugin : IPlugin
{
    public const string TodoRoleName = "RobotCrawlers";
    public const string TodoRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{TodoRoleName}";

    #region Variables

    private readonly IUiHost _uiHost;
    private readonly IAuthorizeHost _authHost;
    private WebApplication _app = null!;

    #endregion

    #region Metadata Properties

    public string Name => "TodoPlugin";

    public string Description => "";

    public string Author => "versx";

    public Version Version => new(1, 0, 0);

    #endregion

    #region Constructor

    public TodoPlugin(IUiHost uiHost, IAuthorizeHost authHost)
    {
        _uiHost = uiHost;
        _authHost = authHost;
    }

    #endregion

    #region ASP.NET Methods

    public void Configure(WebApplication appBuilder)
    {
        _app = appBuilder;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<TodoDbContext>(options => options.UseInMemoryDatabase("todo"), ServiceLifetime.Scoped);
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
            Text = "Todos",
            ControllerName = "Todo",
            ActionName = "Index",
            DisplayIndex = 999,
            Icon = "fa-solid fa-fw fa-list",
        };
        await _uiHost.AddSidebarItemAsync(navbarHeader);

        //var tile = new DashboardTile("Todos", "0", "fa-solid fa-fw fa-list", "Todo");
        var tile = new DashboardTile("Todos", "fa-solid fa-fw fa-list", "Todo", valueUpdater: new Func<string>(() =>
        {
            using var scope = _app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            var count = context.Todos.Count().ToString("N0");
            return count;
        }));
        await _uiHost.AddDashboardTileAsync(tile);

        await _authHost.RegisterRole(TodoRoleName, 2);
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