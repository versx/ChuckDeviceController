namespace ChuckDeviceController.Middleware;

using ChuckDeviceController.Extensions;

public sealed class MadDataMiddleware
{
    private readonly RequestDelegate _next;

    public MadDataMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await context.ConvertPayloadDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error - MadDataMiddleware: {ex.Message}");
        }

        await _next(context);
    }
}