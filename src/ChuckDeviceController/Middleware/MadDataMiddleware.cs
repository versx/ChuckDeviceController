namespace ChuckDeviceController.Middleware;

using ChuckDeviceController.Extensions;

public sealed class MadDataMiddleware
{
    private readonly ILogger<MadDataMiddleware> _logger;
    private readonly RequestDelegate _next;

    public MadDataMiddleware(ILogger<MadDataMiddleware> logger, RequestDelegate next)
    {
        _logger = logger;
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
            _logger.LogError($"MadDataMiddleware: {ex.Message}");
        }

        await _next(context);
    }
}