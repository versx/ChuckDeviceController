namespace RobotsPlugin.Extensions
{
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Infrastructure;

    public static class ActionDescriptorCollectionProviderExtensions
    {
        public static ActionDescriptor? GetRouteActionDescriptor(this IActionDescriptorCollectionProvider routeProvider, string routeName)
        {
            var route = routeProvider.ActionDescriptors.Items.FirstOrDefault(ad =>
                ad.DisplayName.StartsWith(routeName, StringComparison.CurrentCultureIgnoreCase));
            return route;
        }
    }
}