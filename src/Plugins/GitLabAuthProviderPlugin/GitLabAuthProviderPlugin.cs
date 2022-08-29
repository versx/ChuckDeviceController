﻿namespace GitLabAuthProviderPlugin
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Plugin;

    public class GitLabAuthProviderPlugin : IPlugin
    {
        #region Variables

        private readonly IConfiguration _config;

        #endregion

        #region Metadata Properties

        public string Name => "GitLabAuthProviderPlugin";

        public string Description => "Adds GitLab.com authentication provider to possible 3rd party authentication providers.";

        public string Author => "versx";

        public Version Version => new(1, 0, 0);

        #endregion

        #region Constructor

        public GitLabAuthProviderPlugin(IConfigurationHost configHost)
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
            var authBuilder = services.AddAuthentication();

            if (_config.GetValue<bool>("GitLab:Enabled"))
            {
                var clientId = _config.GetValue<string>("GitLab:ClientId");
                var clientSecret = _config.GetValue<string>("GitLab:ClientSecret");
                authBuilder.AddGitLab(options =>
                {
                    options.ClientId = clientId!;
                    options.ClientSecret = clientSecret!;
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
}