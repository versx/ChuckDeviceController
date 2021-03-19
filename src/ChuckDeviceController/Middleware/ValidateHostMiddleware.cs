namespace ChuckDeviceController.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using ChuckDeviceController.Extensions;

    // TODO: Load all valid host IP addresses from database
    public class ValidateHostMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidateHostMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var validList = new List<string>();
            var host = httpContext.Request.GetIPAddress();
            if (validList.Count > 0 && !validList.Contains(host))
            {
                // Invalid
                httpContext.Response.StatusCode = 401;
            }
            else
            {
                await _next(httpContext).ConfigureAwait(false);
            }
        }
    }
}