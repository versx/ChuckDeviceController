namespace RequestBenchmarkPlugin.Middleware;

using Microsoft.Extensions.Options;

using Data.Contexts;
using Data.Entities;
using Services;
using Utilities;

public sealed class RequestBenchmarkMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestBenchmarkService _benchmarkService;
    private readonly RequestBenchmarkConfig _config;

    public RequestBenchmarkMiddleware(
        RequestDelegate next,
        IRequestBenchmarkService benchmarkService,
        IOptions<RequestBenchmarkConfig> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _benchmarkService = benchmarkService ?? throw new ArgumentNullException(nameof(benchmarkService));
        _config = options?.Value ?? new();
    }

    public async Task InvokeAsync(HttpContext context, RequestTimesDbContext dbContext)
    {
        if (context.Request.ContentType == "application/grpc" &&
            _config.IgnoreGrpcRequests)
        {
            await _next(context);
            return;
        }

        // TODO: Add config option to ignore custom defined paths

        var route = context.Request.Path.ToString();
        var benchmark = _benchmarkService.Benchmarks.ContainsKey(route)
            ? _benchmarkService.Benchmarks[route]
            : new(route);

        using (StopwatchTimer.Initialize(benchmark))
        {
            await _next(context);
        }

        var timing = RequestTime.FromRequestBenchmark(benchmark);
        await _benchmarkService.SaveRouteBenchmark(dbContext, route, timing);

        _benchmarkService.UpdateRouteBenchmark(route, benchmark);
    }
}