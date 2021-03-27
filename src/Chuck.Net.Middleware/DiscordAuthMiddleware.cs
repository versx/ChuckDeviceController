namespace Chuck.Net.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using Chuck.Net.Extensions;

    public class DiscordAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public DiscordAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var ignorePaths = new List<string>
            {
                "/discord/login",
                "/discord/callback",
                "/controler",
                "/controller",
                //"/health-ui",
                "/health",
            };
            if (!httpContext.Session.GetValue<bool>("is_valid") && !ignorePaths.Contains(httpContext.Request.Path))
            {
                httpContext.Session.SetValue("last_redirect", httpContext.Request.Path.Value);
                httpContext.Response.Redirect("/discord/login");
            }
            else
            {
                await _next(httpContext).ConfigureAwait(false);
            }
        }
    }
}