namespace ChuckDeviceController.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    // TODO: Load all valid tokens from database
    public class TokenAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var validList = new List<string>();
            var token = httpContext.Request.Headers["Authorization"].ToString();
            if (validList.Count > 0 && !validList.Contains(token))
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