namespace MicrosoftAccountAuthProviderPlugin;

using Microsoft.Extensions.Configuration;

using ChuckDeviceController.Common.Configuration;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Plugin;

[PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
public class MicrosoftAccountAuthProviderPlugin : IPlugin
{
    #region Variables

    private readonly IConfiguration _config;

    #endregion

    #region Plugin Metadata Properties

    /// <summary>
    /// Gets the name of the plugin to use.
    /// </summary>
    public string Name => "MicrosoftAccountAuthProviderPlugin";

    /// <summary>
    /// Gets a brief description about the plugin explaining how it
    /// works and what it does.
    /// </summary>
    public string Description => "Adds Microsoft account authentication provider to possible 3rd party authentication providers.";

    /// <summary>
    /// Gets the name of the author/creator of the plugin.
    /// </summary>
    public string Author => "versx";

    /// <summary>
    /// Gets the current version of the plugin.
    /// </summary>
    public Version Version => new(1, 0, 0);

    #endregion

    #region Constructor

    public MicrosoftAccountAuthProviderPlugin(IConfigurationHost configHost)
    {
        _config = configHost.GetConfiguration();
    }

    #endregion

    #region ASP.NET WebApi Configure Callback Handlers

    public void Configure(WebApplication appBuilder)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var config = new OpenAuthConfig();
        _config.GetSection("Microsoft").Bind(config);

        var authBuilder = services.AddAuthentication();

        if (config.Enabled)
        {
            authBuilder.AddMicrosoftAccount(options =>
            {
                options.ClientId = config.ClientId!;
                options.ClientSecret = config.ClientSecret!;
            });
        }
    }

    public void ConfigureMvcBuilder(IMvcBuilder mvcBuilder)
    {
    }

    #endregion

    #region Plugin Event Handlers

    public void OnLoad()
    {
    }

    public void OnReload()
    {
    }

    public void OnStop()
    {
    }

    public void OnRemove()
    {
    }

    public void OnStateChanged(PluginState state)
    {
    }

    #endregion
}