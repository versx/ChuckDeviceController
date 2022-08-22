namespace RobotsPlugin.Services
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Infrastructure;

    using Data.Contracts;
    using Extensions;

    using ChuckDeviceController.Plugin.Services;

    // TODO: Move to host?

    [
        PluginService(
            ServiceType = typeof(IRouteDataService),
            ProxyType = typeof(RouteDataService),
            Provider = PluginServiceProvider.Plugin,
            Lifetime = ServiceLifetime.Scoped
        )
    ]
    public class RouteDataService : IRouteDataService
    {
        public string GetRouteFromClass(Type type, IActionDescriptorCollectionProvider routeProvider)
        {
            var classRouteAttribute = type.GetRobotAttribute<RouteAttribute>();
            if (classRouteAttribute != null && !string.IsNullOrEmpty(classRouteAttribute.Template))
            {
                return classRouteAttribute.Template;
            }

            var route = routeProvider.GetRouteActionDescriptor(type.FullName);
            if (route == null)
            {
                return string.Empty;
            }

            if (route.AttributeRouteInfo != null)
            {
                return $"/{route.AttributeRouteInfo.Template}/{route.AttributeRouteInfo.Name}";
            }
            else if (route.AttributeRouteInfo == null)
            {
                var controllerDescriptor = route as ControllerActionDescriptor;
                return $"/{controllerDescriptor.ControllerName}";
            }

            return string.Empty;
        }

        public string GetRouteFromMethod(MethodInfo method, IActionDescriptorCollectionProvider routeProvider)
        {
            // does the class have a route attribute
            var classRouteAttribute = method.GetType().GetRobotAttribute<RouteAttribute>();
            if (classRouteAttribute != null && !string.IsNullOrEmpty(classRouteAttribute.Template))
            {
                var template = classRouteAttribute.Template;
                while (template.IndexOf('{') > -1)
                {
                    template = template[..^1];
                }

                if (template.EndsWith("/"))
                {
                    template = template[..^1];
                }

                return template;
            }

            var routeName = $"{method.DeclaringType}.{method.Name}";
            var route = routeProvider.GetRouteActionDescriptor(routeName);
            if (route == null)
            {
                return string.Empty;
            }

            if (route.AttributeRouteInfo != null)
            {
                return $"/{route.AttributeRouteInfo.Template}/{route.AttributeRouteInfo.Name}";
            }

            if (route.RouteValues["controller"].ToString() == "Home")
            {
                return $"/{route.RouteValues["action"]}";
            }
            else
            {
                return $"/{route.RouteValues["controller"]}/{route.RouteValues["action"]}";
            }
        }
    }
}