namespace RequestBenchmarkPlugin.Middleware
{
    using Data.Contexts;
    using Data.Entities;
    using Services;
    using Utilities;

    public sealed class RequestBenchmarkMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestBenchmarkService _benchmarkService;

        public RequestBenchmarkMiddleware(RequestDelegate next, IRequestBenchmarkService benchmarkService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _benchmarkService = benchmarkService ?? throw new ArgumentNullException(nameof(benchmarkService));
        }

        public async Task InvokeAsync(HttpContext context, RequestTimesDbContext dbContext)
        {
            // TODO: Make configurable to ignore gRPC requests
            if (context.Request.ContentType == "application/grpc")
            {
                await _next(context);
                return;
            }

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
}