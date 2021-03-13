namespace ChuckDeviceController.Extensions
{
    using Microsoft.AspNetCore.Builder;

    using ChuckDeviceController.Middleware;

    public static class DiscordAuthExtensions
    {
        public static IApplicationBuilder UseDiscordAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DiscordAuthMiddleware>();
        }
    }
}