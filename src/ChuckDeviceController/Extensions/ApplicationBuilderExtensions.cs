namespace ChuckDeviceController.Extensions
{
    using Microsoft.AspNetCore.Builder;

    using ChuckDeviceController.Middleware;

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDiscordAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DiscordAuthMiddleware>();
        }

        public static IApplicationBuilder UseHostWhitelist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ValidateHostMiddleware>();
        }

        public static IApplicationBuilder UseTokenAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenAuthMiddleware>();
        }
    }
}