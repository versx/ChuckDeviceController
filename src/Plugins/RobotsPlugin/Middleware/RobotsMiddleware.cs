namespace RobotsPlugin.Middleware
{
    using System.Text;

    using Microsoft.Extensions.Options;

    using Configuration;
    using Data.Contracts;
    using Extensions;
    using Services;

    /// <summary>
    /// Web crawler robot middleware, serves robots.txt on request and denies access to
    /// route for robot connections.
    /// </summary>
    public sealed class RobotsMiddleware
    {
        #region Constants

        private static readonly IEnumerable<string> DefaultStaticFileExtensions = new List<string>
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
        private const int HttpResponseSuccess = 200;
        private const int HttpResponseForbidden = 403;
        private const int HttpResponseNotAllowed = 405;
        private const string RobotsTxtRoute = "/robots.txt";
        private const string UserAgentHeader = "User-Agent";

        #endregion

        #region Variables

        private readonly RequestDelegate _next;
        private readonly IRobots _robots;
        private readonly IHoneyPotService _honeyPotService;

        #endregion

        #region Properties

        public WebCrawlerConfig Options { get; }

        #endregion

        #region Constructor

        public RobotsMiddleware(
            RequestDelegate next,
            IRobots robots,
            IHoneyPotService honeyPotService,
            IOptions<WebCrawlerConfig> optionsAccessor)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _robots = robots ?? throw new ArgumentNullException(nameof(robots));
            _honeyPotService = honeyPotService ?? throw new ArgumentNullException(nameof(honeyPotService));

            Options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
            if (Options.ProcessStaticFiles && !Options.StaticFileExtensions.Any())
            {
                Options.StaticFileExtensions.AddRange(DefaultStaticFileExtensions);
            }
        }

        #endregion

        #region Public Methods

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Check if request is to route for static file
            var ext = context.GetRouteFileExtension();
            if (!Options.ProcessStaticFiles &&
                !string.IsNullOrEmpty(ext) &&
                Options.StaticFileExtensions.Contains(ext))
            {
                await _next(context);
                return;
            }

            var route = context.GetRouteLowered();
            if (Options.UseHoneyPotService &&
                !string.IsNullOrEmpty(Options.HoneyPotRoute)
                && route.StartsWith(Options.HoneyPotRoute))
            {
                // Robot found honey pot route, log UserAgent and IP to keep track of
                await HandleHoneyPotRouteResponse(context);
                return;
            }
            else if (route.EndsWith(RobotsTxtRoute))
            {
                // Generate robots.txt file from configured UserAgents and routes
                await HandleRobotsTxtRouteResponse(context);
                return;
            }
            else
            {
                // Check if request is from unknown crawler not declaring itself
                if (context.Request.IsDeclaredBotCrawler())
                {
                    // If so, check if bot trying to visit a denied route
                    var isDenied = await HandleRobotDeniedRouteResponse(context, route);
                    if (isDenied)
                    {
                        return;
                    }
                }
            }

            await _next(context);
        }

        #endregion

        #region Route Handlers

        private async Task HandleHoneyPotRouteResponse(HttpContext context)
        {
            context.Response.StatusCode = HttpResponseNotAllowed;

            var ipAddr = context.Request.GetIPAddress();
            var userAgent = Convert.ToString(context.Request.Headers[UserAgentHeader]);
            _honeyPotService.OnTriggered(ipAddr, userAgent);
            await Task.CompletedTask;
        }

        private async Task HandleRobotsTxtRouteResponse(HttpContext context)
        {
            context.Response.StatusCode = HttpResponseSuccess;

            var data = GenerateRobotsResponse();
            var response = Encoding.UTF8.GetBytes(data);
            await context.Response.Body.WriteAsync(response);
        }

        private async Task<bool> HandleRobotDeniedRouteResponse(HttpContext context, string route)
        {
            var isDenied = false;
            var userAgent = context.Request.GetUserAgent(toLower: true);
            foreach (var deniedRoute in _robots.DeniedRoutes)
            {
                if (deniedRoute.Route.StartsWith(route) &&
                    (
                        // Deny all requests with UserAgent
                        deniedRoute.UserAgent == "*" ||
                        // Deny specific requests
                        userAgent.Contains(deniedRoute.UserAgent.ToLower())
                    ))
                {
                    context.Response.StatusCode = HttpResponseForbidden;
                    isDenied = true;
                    break;
                }
            }

            return await Task.FromResult(isDenied);
        }

        #endregion

        #region Helper Methods

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

        #endregion
    }
}