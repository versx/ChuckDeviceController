namespace RobotsPlugin.Extensions
{
    // TODO: Move to PluginHelper library that can be referenced for plugin writers
    public static class HttpContextExtensions
    {
        private const string LoweredRoute = "RouteLowered";
        private const string RouteNormal = "Route";
        private const string ExtensionLowered = "ExtensionLowered";

        /// <summary>
        /// Retrieves the file extension for the file requested in the current request in lowercase.
        /// </summary>
        /// <param name="context">Valid HttpContext for the request.</param>
        /// <returns></returns>
        public static string? GetRouteFileExtension(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Items.ContainsKey(ExtensionLowered))
            {
                return Convert.ToString(context.Items[ExtensionLowered]);
            }

            var loweredExtension = Path.GetExtension(GetRouteLowered(context));
            context.Items.Add(ExtensionLowered, loweredExtension);
            return loweredExtension;
        }

        /// <summary>
        /// Retrieves the current route being requested through the pipeline in lowercase.
        /// </summary>
        /// <param name="context">Valid HttpContext for the request.</param>
        /// <returns></returns>
        public static string? GetRouteLowered(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Items.ContainsKey(LoweredRoute))
            {
                return Convert.ToString(context.Items[LoweredRoute]);
            }

            var routeLowered = context.GetRoute()?.ToLowerInvariant();
            if (routeLowered != null)
            {
                context.Items.Add(LoweredRoute, routeLowered);
            }
            return routeLowered;
        }

        /// <summary>
        /// Retrieves the current route being requested through the pipeline.
        /// </summary>
        /// <param name="context">Valid HttpContext for the request.</param>
        /// <returns></returns>
        public static string? GetRoute(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Items.ContainsKey(RouteNormal))
            {
                return Convert.ToString(context.Items[RouteNormal]);
            }

            var route = context.Request.Path.ToString();
            context.Items.Add(RouteNormal, route);
            return route;
        }
    }
}