namespace RobotsPlugin.Middleware
{
    using System.Text;

    using Data.Contracts;
    using Extensions;

    /// <summary>
    /// Web crawler robot middleware, serves robots.txt on request and denies access to
    /// route for robot connections.
    /// </summary>
    public sealed class RobotsMiddleware
    {
        private static readonly IEnumerable<string> StaticFileExtensions = new List<string>
        {
            ".less",
            ".ico",
            ".css",
            ".js",
            ".svg",
            ".jpg",
            ".jpeg",
            ".gif",
            ".png",
            ".eot",
            ".map;",
        };
        private const int HtmlResponseSuccess = 200;
        private const string RobotsTxtRoute = "/robots.txt";

        private readonly RequestDelegate _next;
        private readonly IRobots _robots;

        public bool ProcessStaticFiles { get; set; }

        public RobotsMiddleware(RequestDelegate next, IRobots robots)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _robots = robots ?? throw new ArgumentNullException(nameof(robots));

            ProcessStaticFiles = true; // TODO: Make configurable
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var ext = context.GetRouteFileExtension();
            if (!ProcessStaticFiles &&
                !string.IsNullOrEmpty(ext) &&
                StaticFileExtensions.Contains(ext))
            {
                await _next(context);
                return;
            }

            var route = context.GetRouteLowered();
            if (route?.EndsWith(RobotsTxtRoute) ?? false)
            {
                context.Response.StatusCode = HtmlResponseSuccess;

                var data = GenerateRobotsResponse();
                var response = Encoding.UTF8.GetBytes(data);
                await context.Response.Body.WriteAsync(response);
                return;
            }

            await _next(context);
        }

        private string GenerateRobotsResponse()
        {
            if (!_robots.UserAgents.Any())
            {
                var response = "# Allow all web crawlers\r\n\r\nUser-agent: *\r\nAllow: /\r\n";
                return response;
            }

            var sb = new StringBuilder();
            var lastAgent = string.Empty;
            foreach (var userAgent in _robots.UserAgents)
            {
                var routes = _robots.GetRoutes(userAgent);
                if (!routes.Any())
                    continue;

                if (!lastAgent.Equals(userAgent))
                {
                    lastAgent = userAgent;
                    sb.Append($"\r\nUser-agent: {lastAgent}\r\n");
                }

                foreach (var value in routes)
                {
                    sb.Append($"{value}\r\n");
                }
            }
            return sb.ToString();
        }
    }
}