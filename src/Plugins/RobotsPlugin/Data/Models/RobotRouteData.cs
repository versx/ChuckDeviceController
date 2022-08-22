namespace RobotsPlugin.Data.Models
{
    using Contracts;

    using ChuckDeviceController.Plugin.Services;

    [
        PluginService(
            ServiceType = typeof(IRobotRouteData),
            ProxyType = typeof(RobotRouteData),
            Provider = PluginServiceProvider.Plugin,
            Lifetime = ServiceLifetime.Scoped
        )
    ]
    public class RobotRouteData : IRobotRouteData
    {
        #region Properties

        public string UserAgent { get; set; }

        public string? Comment { get; set; }

        public string Route { get; set; }

        public bool IsAllowed { get; set; }

        public bool IsCustom { get; set; }

        #endregion

        #region Constructors

        public RobotRouteData()
        {
        }

        public RobotRouteData(string userAgent, string route, string? comment = null, bool isAllowed = false, bool isCustom = false)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentNullException(nameof(userAgent));
            }
            if (string.IsNullOrEmpty(route))
            {
                throw new ArgumentNullException(nameof(route));
            }
            if (!Uri.TryCreate(route, UriKind.Relative, out _))
            {
                throw new ArgumentException($"Route '{route}' must be a partial Uri", nameof(route));
            }

            UserAgent = userAgent;
            Comment = comment ?? string.Empty;
            Route = route;
            IsAllowed = isAllowed;
            IsCustom = isCustom;
        }

        #endregion
    }
}