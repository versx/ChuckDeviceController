namespace DeviceAuthPlugin.Middleware
{
    using System.Net;

    using Microsoft.Extensions.Options;

    using Configuration;

    public sealed class TokenAuthMiddleware
    {
        // TODO: Make 'AffectedRoutes' configurable
        private static readonly IEnumerable<string> AffectedRoutes = new List<string>
        {
            "/controller",
            "/controler",
            "/raw",
        };
        private readonly RequestDelegate _next;

        public TokenAuthConfig Options { get; }

        public TokenAuthMiddleware(
            RequestDelegate next,
            IOptions<TokenAuthConfig> optionsAccessor)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            Options = optionsAccessor.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLower();
            if (!AffectedRoutes.Any(route => route == path))
            {
                await _next(context);
                return;
            }

            // Deny all requests if no tokens set but middleware is enabled
            if (!Options.Tokens.Any())
            {
                return;
            }

            // Extract 'Authorization' header to get device auth token
            if (!context.Request.Headers.TryGetValue(HttpRequestHeader.Authorization.ToString(), out var header))
            {
                return;
            }

            var token = Convert.ToString(header);
            if (!Options.Tokens.Contains(token))
            {
                return;
            }

            await _next(context);
        }
    }
}