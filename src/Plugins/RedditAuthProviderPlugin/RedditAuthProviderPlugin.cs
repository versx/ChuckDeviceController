namespace RedditAuthProviderPlugin;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using ChuckDeviceController.Common.Configuration;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Plugin;

[PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
public class RedditAuthProviderPlugin : IPlugin
{
    #region Variables

    private readonly IConfiguration _config;

    #endregion

    #region Metadata Properties

    public string Name => "RedditAuthProviderPlugin";

    public string Description => "Adds Reddit.com authentication provider to possible 3rd party authentication providers.";

    public string Author => "versx";

    public Version Version => new(1, 0, 0);

    #endregion

    #region Constructor

    public RedditAuthProviderPlugin(IConfigurationHost configHost)
    {
        _config = configHost.GetConfiguration();
    }

    #endregion

    #region ASP.NET Methods

    public void Configure(WebApplication appBuilder)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var config = new OpenAuthConfig();
        _config.GetSection("Reddit").Bind(config);

        var authBuilder = services.AddAuthentication();

        if (config.Enabled)
        {
            authBuilder.AddReddit(options =>
            {
                options.ClientId = config.ClientId!;
                options.ClientSecret = config.ClientSecret!;
                //options.Scope("");
            });
        }
    }

    public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
    {
    }

    #endregion

    #region Plugin Methods

    public void OnLoad()
    {
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