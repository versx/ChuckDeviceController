namespace RobotsPlugin.Extensions
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Infrastructure;

    public static class ActionDescriptorCollectionProviderExtensions
    {
        public static ActionDescriptor? GetRouteActionDescriptor(this IActionDescriptorCollectionProvider routeProvider, string routeName)
        {
            var route = routeProvider.ActionDescriptors.Items.FirstOrDefault(ad =>
                ad.DisplayName!.StartsWith(routeName, StringComparison.CurrentCultureIgnoreCase));
            return route;
        }

        /// <summary>
        /// Provides the route associated with an action method, this will be based on the name of the action and 
        /// controller and if supplied the Route attributes placed on the class and method in question.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="routeProvider">IActionDescriptorCollectionProvider instance obtained using DI.</param>
        /// <returns></returns>
        public static string GetRouteFromMethod(this IActionDescriptorCollectionProvider routeProvider, MethodInfo method)
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

            if (route.RouteValues["controller"]?.ToString() == "Home")
            {
                return $"/{route.RouteValues["action"]}";
            }
            else
            {
                return $"/{route.RouteValues["controller"]}/{route.RouteValues["action"]}";
            }
        }

        /// <summary>
        /// Provides the route associated with a class, this will be based on the controller name
        /// and if supplied the Route attributes placed on the class.
        /// </summary>
        /// <param name="type">Type to be checked for route data.</param>
        /// <param name="routeProvider">IActionDescriptorCollectionProvider instance obtained using DI.</param>
        /// <returns></returns>
        public static string GetRouteFromClass(this IActionDescriptorCollectionProvider routeProvider, Type type)
        {
            var classRouteAttribute = type.GetRobotAttribute<RouteAttribute>();
            if (classRouteAttribute != null && !string.IsNullOrEmpty(classRouteAttribute.Template))
            {
                return classRouteAttribute.Template;
            }

            var route = routeProvider.GetRouteActionDescriptor(type.FullName!);
            if (route == null)
            {
                return string.Empty;
            }

            if (route.AttributeRouteInfo != null)
            {
                return $"/{route.AttributeRouteInfo.Template}/{route.AttributeRouteInfo.Name}";
            }

            if (route is ControllerActionDescriptor controllerDescriptor)
            {
                return $"/{controllerDescriptor.ControllerName}";
            }

            return string.Empty;
        }
    }
}