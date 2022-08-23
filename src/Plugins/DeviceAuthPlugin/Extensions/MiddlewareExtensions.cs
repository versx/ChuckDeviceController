namespace DeviceAuthPlugin.Extensions
{
    using Middleware;

    public static class MiddlewareExtensions
    {
        public static void UseTokenAuth(this IApplicationBuilder builder, params object?[] args)
        {
            builder.UseMiddleware<TokenAuthMiddleware>(args);
        }

        public static void UseIpAddressAuth(this IApplicationBuilder builder, params object?[] args)
        {
            builder.UseMiddleware<IpAddressAuthMiddleware>(args);
        }
    }
}