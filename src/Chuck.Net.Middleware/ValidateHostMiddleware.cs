namespace Chuck.Net.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using Chuck.Net.Extensions;

    public class ValidateHostMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<string> _validHosts;

        public ValidateHostMiddleware(RequestDelegate next, List<string> validHosts)
        {
            _next = next;
            _validHosts = validHosts;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var host = httpContext.Request.GetIPAddress();
            if (_validHosts?.Count > 0 && !_validHosts.Contains(host))
            {
                // Invalid
                httpContext.Response.StatusCode = 401;
                return;
            }
            else
            {
                await _next(httpContext).ConfigureAwait(false);
            }
        }
    }
}