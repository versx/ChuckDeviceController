namespace RobotsPlugin.Extensions;

using Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRobots(this IApplicationBuilder builder, params object?[] args)
    {
        return builder.UseMiddleware<RobotsMiddleware>(args);
    }
}