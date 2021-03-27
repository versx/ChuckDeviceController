namespace Chuck.Net.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    public class TokenAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<string> _validTokens;

        public TokenAuthMiddleware(RequestDelegate next, List<string> validTokens)
        {
            _next = next;
            _validTokens = validTokens;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var token = httpContext.Request.Headers["Authorization"].ToString();
            if (_validTokens?.Count > 0 && !_validTokens.Contains(token))
            {
                // Invalid
                httpContext.Response.StatusCode = 401;
                return;
            }
            await _next(httpContext).ConfigureAwait(false);
        }
    }
}