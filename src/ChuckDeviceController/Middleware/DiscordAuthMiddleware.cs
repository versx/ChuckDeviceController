namespace ChuckDeviceController.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using ChuckDeviceController.Controllers;
    using ChuckDeviceController.Extensions;

    public class DiscordAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public DiscordAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // TODO: Fix
            /*
            if (!DiscordController.Enabled)
            {
                await _next(httpContext);
                return;
            }
            */
            var ignorePaths = new List<string>
            {
                "/discord/login",
                "/discord/callback",
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