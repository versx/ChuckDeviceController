namespace DeviceAuthPlugin.Middleware
{
    using Microsoft.Extensions.Options;

    using Configuration;
    using Extensions;

    public sealed class IpAddressAuthMiddleware
    {
        private static readonly IEnumerable<string> AffectedRoutes = new List<string>
        {
            "/controller",
            "/controler",
            "/raw",
        };
        private readonly RequestDelegate _next;

        public IpAuthConfig Options { get; }

        public IpAddressAuthMiddleware(
            RequestDelegate next,
            IOptions<IpAuthConfig> optionsAccessor)
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

            // Deny all requests if no IP addresses set but middleware is enabled
            if (!Options.IpAddresses.Any())
            {
                return;
            }

            var ipAddress = context.Request.GetIPAddress();
            if (!Options.IpAddresses.Contains(ipAddress))
            {
                return;
            }

            await _next(context);
        }
    }
}