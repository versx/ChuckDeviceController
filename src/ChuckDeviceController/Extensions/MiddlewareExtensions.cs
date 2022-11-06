namespace ChuckDeviceController.Extensions
{
    using ChuckDeviceController.Middleware;

    public static class MiddlewareExtensions
    {
        public static void UseMadDataConverter(this IApplicationBuilder builder, params object?[] args)
        {
            builder.UseWhen(context =>
                context.IsRawDataRequest() &&
                context.IsPostRequest() &&
                context.IsOriginHeaderSet(),
                appBuilder =>
            {
                appBuilder.UseMiddleware<MadDataMiddleware>(args);
            });
        }
    }
}