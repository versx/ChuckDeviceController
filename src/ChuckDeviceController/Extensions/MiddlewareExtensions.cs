namespace ChuckDeviceController.Extensions
{
    using ChuckDeviceController.Middleware;

    public static class MiddlewareExtensions
    {
        public static void UseMadData(this IApplicationBuilder builder, params object?[] args)
        {
            builder.UseMiddleware<MadDataMiddleware>(args);
        }
    }
}