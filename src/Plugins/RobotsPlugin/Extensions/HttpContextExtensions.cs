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

            var loweredExtension = Path.GetExtension(GetRouteLowered(context));
            context.Items.Add(ExtensionLowered, loweredExtension);
            return loweredExtension;
        }

        /// <summary>
        /// Retrieves the current route being requested through the pipeline in lowercase.
        /// </summary>
        /// <param name="context">Valid HttpContext for the request.</param>
        /// <returns></returns>
        public static string GetRouteLowered(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Items.ContainsKey(LoweredRoute))
            {
                return context.Items[LoweredRoute]?.ToString() ?? string.Empty;
            }

            var routeLowered = context.GetRoute().ToLowerInvariant();
            context.Items.Add(LoweredRoute, routeLowered);
            return routeLowered ?? string.Empty;
        }

        /// <summary>
        /// Retrieves the current route being requested through the pipeline.
        /// </summary>
        /// <param name="context">Valid HttpContext for the request.</param>
        /// <returns></returns>
        public static string GetRoute(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Items.ContainsKey(RouteNormal))
            {
                return context.Items[RouteNormal]?.ToString() ?? string.Empty;
            }

            var route = context.Request.Path.ToString();
            context.Items.Add(RouteNormal, route);
            return route;
        }
    }
}