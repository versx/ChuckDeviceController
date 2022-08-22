﻿namespace RobotsPlugin.Data.Models
{
    using Microsoft.AspNetCore.Mvc.Infrastructure;

    using Attributes;
    using Contracts;
    using Extensions;

    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.Plugin.Services;

    [
        PluginService(
            ServiceType = typeof(IRobots),
            ProxyType = typeof(Robots),
            Provider = PluginServiceProvider.Plugin,
            Lifetime = ServiceLifetime.Scoped
        )
    ]
    public class Robots : IRobots
    {
        #region Constants

        private const string DefaultRobotsFileName = "robots.txt";
        private const string DefaultRobotsFolderName = "";

        #endregion

        #region Variables

        private readonly Dictionary<string, List<IRobotRouteData>> _userAgents;
        private readonly List<DeniedRoute> _deniedRobotRoutes;
        private readonly IFileStorageHost _storageHost;
        private List<RobotRouteData> _customRoutes;

        #endregion

        #region Constructors

        public Robots(
            IActionDescriptorCollectionProvider routeProvider,
            IRouteDataService routeDataService,
            IFileStorageHost storageHost)
        {
            if (routeProvider == null)
            {
                throw new ArgumentNullException(nameof(routeProvider));
            }
            if (routeDataService == null)
            {
                throw new ArgumentNullException(nameof(routeDataService));
            }

            _deniedRobotRoutes = new List<DeniedRoute>();
            _userAgents = LoadRobotData(routeProvider, routeDataService);
            _customRoutes = new List<RobotRouteData>();
            _storageHost = storageHost ?? throw new ArgumentNullException(nameof(storageHost));

            LoadData();
        }

        #endregion

        #region Properties

        public IEnumerable<string> UserAgents => _userAgents.Keys.ToList();

        public IEnumerable<DeniedRoute> DeniedRoutes => _deniedRobotRoutes;

        public IEnumerable<IRobotRouteData> CustomRoutes => _customRoutes;

        #endregion

        #region Public Methods

        #region User Agents

        public bool AddUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentNullException(nameof(userAgent));
            }

            if (_userAgents.ContainsKey(userAgent))
                return false;

            _userAgents.Add(userAgent, new());

            return true;
        }

        public bool RemoveUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentNullException(nameof(userAgent));
            }

            if (!_userAgents.ContainsKey(userAgent))
                return false;

            if (_userAgents[userAgent].Any(item => !item.IsCustom))
                return false;

            return _userAgents.Remove(userAgent);
        }

        #endregion

        #region Routes

        public IEnumerable<string> GetRoutes(string userAgent)
        {
            var result = new List<string>();
            if (!_userAgents.ContainsKey(userAgent))
                return result;

            var userAgentRouteData = _userAgents[userAgent];
            foreach (var routeData in userAgentRouteData)
            {
                if (!string.IsNullOrEmpty(routeData.Comment))
                {
                    result.Add($"#{routeData.Comment}");
                }

                if (routeData.IsAllowed)
                    result.Add($"Allow: {routeData.Route}");
                else
                    result.Add($"Disallow: {routeData.Route}");
            }
            return result;
        }

        public bool AddRoute(string userAgent, string route, bool isAllowed, string? comment = null)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentNullException(nameof(userAgent));
            }
            if (string.IsNullOrEmpty(route))
            {
                throw new ArgumentNullException(nameof(route));
            }
            if (!UserAgents.Contains(userAgent))
            {
                throw new ArgumentException($"UserAgent '{userAgent}' is not registered.", nameof(userAgent));
            }
            if (_customRoutes.Any(r => r.UserAgent.Equals(userAgent) && r.Route.Equals(route)))
            {
                return false;
            }

            _customRoutes.Add(new RobotRouteData(userAgent, route, comment, isAllowed: isAllowed, isCustom: true));
            AddCustomRoutesToKnownAgents();

            return true;
        }

        public bool UpdateRoute(string userAgent, string route, bool isAllowed, string? comment = null)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentNullException(nameof(userAgent));
            }
            if (string.IsNullOrEmpty(route))
            {
                throw new ArgumentNullException(nameof(route));
            }
            if (!UserAgents.Contains(userAgent))
            {
                throw new ArgumentException($"UserAgent '{userAgent}' is not registered.", nameof(userAgent));
            }

            var customRoute = _customRoutes.FirstOrDefault(r => r.UserAgent.Equals(userAgent) && r.Route.Equals(route));
            if (customRoute == null)
                return false;

            var index = _customRoutes.IndexOf(customRoute);
            if (index > -1)
            {
                customRoute.Comment = comment ?? null;
                customRoute.IsAllowed = isAllowed;

                _customRoutes[index] = customRoute;
                AddCustomRoutesToKnownAgents();

                return true;
            }

            return false;
        }

        public bool RemoveRoute(string userAgent, string route)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentNullException(nameof(userAgent));
            }
            if (string.IsNullOrEmpty(route))
            {
                throw new ArgumentNullException(nameof(route));
            }
            if (!UserAgents.Contains(userAgent))
            {
                throw new ArgumentException($"UserAgent '{userAgent}' is not registered.", nameof(userAgent));
            }

            var customRoute = _customRoutes.FirstOrDefault(r => r.UserAgent.Equals(userAgent) && r.Route.Equals(route));
            if (customRoute == null)
                return false;

            _customRoutes.Remove(customRoute);
            AddCustomRoutesToKnownAgents();

            return true;
        }

        #endregion

        public bool SaveData()
        {
            if (_storageHost == null)
            {
                throw new ArgumentNullException(nameof(_storageHost));
            }

            return _storageHost.Save(
                _customRoutes,
                DefaultRobotsFolderName,
                DefaultRobotsFileName
            );
        }

        #endregion

        #region Private Methods

        private void LoadData()
        {
            var customRobots = _storageHost.Load<List<RobotRouteData>>(
                DefaultRobotsFolderName,
                DefaultRobotsFileName
            );
            if (customRobots != null)
            {
                _customRoutes = customRobots;
            }
            AddCustomRoutesToKnownAgents();
        }

        private void AddCustomRoutesToKnownAgents()
        {
            // Remove all routes from UserAgents cache
            foreach (var (userAgent, routeData) in _userAgents)
            {
                for (var i = _userAgents[userAgent].Count - 1; i >= 0; i--)
                {
                    var item = _userAgents[userAgent][i];
                    if (item.IsCustom)
                    {
                        _userAgents[userAgent].RemoveAt(i);
                    }
                }
            }

            // Add routes to UserAgents cache
            foreach (var routeData in _customRoutes)
            {
                if (!_userAgents.ContainsKey(routeData.UserAgent))
                {
                    _userAgents.Add(routeData.UserAgent, new());
                }

                var route = _userAgents[routeData.UserAgent];
                if (route.Any(l => l.IsCustom && l.Route.Equals(routeData.Route)))
                    continue;

                route.Add(routeData);
            }
        }

        private Dictionary<string, List<IRobotRouteData>> LoadRobotData(
            IActionDescriptorCollectionProvider routeProvider,
            IRouteDataService routeDataService)
        {
            // TODO: Get all 'DenyRobotAttributes' from all plugins for denied routes
            var robotAttributes = new List<Type>();
            var result = SortAndFilterDenyRoutesByUserAgent(routeProvider, routeDataService, robotAttributes);
            var lastUserAgent = string.Empty;
            foreach (var (userAgent, routeData) in result)
            {
                if (!lastUserAgent.Equals(userAgent))
                {
                    lastUserAgent = userAgent;
                }

                foreach (var data in routeData)
                {
                    _deniedRobotRoutes.Add(new DeniedRoute($"{data.Route.ToLower()}", data.UserAgent));
                }
            }
            return result;
        }

        private static Dictionary<string, List<IRobotRouteData>> SortAndFilterDenyRoutesByUserAgent(
            IActionDescriptorCollectionProvider routeProvider,
            IRouteDataService routeDataService,
            IEnumerable<Type> robotAttributes)
        {
            // Loop through all classes and methods decorated with the robot attribute
            var result = new Dictionary<string, List<IRobotRouteData>>();
            foreach (var type in robotAttributes)
            {
                var attributes = type.GetRobotAttributes<DenyRobotAttribute>();
                foreach (var attr in attributes)
                {
                    attr.Route = routeDataService.GetRouteFromClass(type, routeProvider);
                    if (string.IsNullOrEmpty(attr.Route))
                        continue;

                    AddUserAgentToDictionary(attr, result);
                }

                // Look for any methods decorated with Disallow attribute
                foreach (var method in type.GetMethods())
                {
                    attributes = method.GetType().GetRobotAttributes<DenyRobotAttribute>();
                    foreach (var attr in attributes)
                    {
                        attr.Route = routeDataService.GetRouteFromMethod(method, routeProvider);
                        if (string.IsNullOrEmpty(attr.Route))
                            continue;

                        AddUserAgentToDictionary(attr, result);
                    }
                }
            }

            return result;
        }

        private static void AddUserAgentToDictionary(
            DenyRobotAttribute denyRobotAttribute,
            Dictionary<string, List<IRobotRouteData>> userAgents)
        {
            if (!userAgents.ContainsKey(denyRobotAttribute.UserAgent))
            {
                userAgents.Add(denyRobotAttribute.UserAgent, new List<IRobotRouteData>());
            }

            var route = denyRobotAttribute.Route;
            if (!route.EndsWith("/"))
            {
                route += "/";
            }

            userAgents[denyRobotAttribute.UserAgent].Add(new RobotRouteData(
                denyRobotAttribute.UserAgent,
                route,
                denyRobotAttribute.Comment
            ));
        }

        #endregion
    }
}