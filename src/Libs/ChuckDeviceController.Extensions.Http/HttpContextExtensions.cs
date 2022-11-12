namespace ChuckDeviceController.Extensions.Http
{
    using Microsoft.AspNetCore.Http;

    public static partial class HttpContextExtensions
    {
        private const string LoweredRoute = "RouteLowered";
        private const string RouteNormal = "Route";
        private const string ExtensionLowered = "ExtensionLowered";

        /// <summary>
        /// Retrieves the file extension for the file requested in the current request in lowercase.
        /// </summary>
        /// <param name="context">Valid HttpContext for the request.</param>
        /// <returns></returns>
        public static string GetRouteFileExtension(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Items.ContainsKey(ExtensionLowered))
            {
                return context.Items[ExtensionLowered]?.ToString() ?? string.Empty;
            }

            var route = context.GetRoute(toLower: true);
            var loweredExtension = Path.GetExtension(route);
            context.Items.Add(ExtensionLowered, loweredExtension);
            return loweredExtension;
        }

        /// <summary>
        /// Retrieves the current route being requested through the pipeline.
        /// </summary>
        /// <param name="context">Valid HttpContext for the request.</param>
        /// <param name="toLower">Determines whether to provide the route as lowercase.</param>
        /// <returns></returns>
        public static string GetRoute(this HttpContext context, bool toLower = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cacheKey = toLower ? LoweredRoute : RouteNormal;
            if (context.Items.ContainsKey(cacheKey))
            {
                return context.Items[cacheKey]?.ToString() ?? string.Empty;
            }

            var route = context.Request.Path.ToString();
            if (toLower)
            {
                route = route.ToLower();
            }
            context.Items.Add(cacheKey, route);
            return route;
        }
    }
}