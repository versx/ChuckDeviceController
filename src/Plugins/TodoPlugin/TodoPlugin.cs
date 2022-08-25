namespace TodoPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    using Data.Contexts;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    [StaticFilesLocation(StaticFilesLocation.Resources, StaticFilesLocation.External)]
    public class TodoPlugin : IPlugin
    {
        private readonly IUiHost _uiHost;

        #region Metadata Properties

        public string Name => "TodoPlugin";

        public string Description => "";

        public string Author => "versx";

        public Version Version => new(1, 0, 0);

        #endregion

        #region Constructor

        public TodoPlugin(IUiHost uiHost)
        {
            _uiHost = uiHost;
        }

        #endregion

        #region ASP.NET Methods

        public void Configure(WebApplication appBuilder)
        {
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
            var navbarHeader = new NavbarHeader
            {
                Text = "Todos",
                ControllerName = "Todo",
                ActionName = "Index",
                DisplayIndex = 999,
                Icon = "fa-solid fa-fw fa-list",
            };
            await _uiHost.AddNavbarHeaderAsync(navbarHeader);
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
}