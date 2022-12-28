namespace ChuckDeviceConfigurator.Middleware;

using System.Diagnostics;

public class PageLoadTimeMiddleware
{
    private readonly RequestDelegate _next;

    public PageLoadTimeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Items.ContainsKey("PageLoadTime"))
        {
            var sw = new Stopwatch();
            sw.Start();
            context.Items.Add("PageLoadTime", sw);
        }

        await _next(context);
    }
}