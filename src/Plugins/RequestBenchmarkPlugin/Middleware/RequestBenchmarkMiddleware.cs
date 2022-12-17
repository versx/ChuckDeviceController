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
        private readonly bool _ignoreGrpcRequests;

        public RequestBenchmarkMiddleware(RequestDelegate next, IRequestBenchmarkService benchmarkService, IConfiguration config)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _benchmarkService = benchmarkService ?? throw new ArgumentNullException(nameof(benchmarkService));
            _ignoreGrpcRequests = config.GetValue<bool>("IgnoreGrpcRequests");
        }

        public async Task InvokeAsync(HttpContext context, RequestTimesDbContext dbContext)
        {
            if (context.Request.ContentType == "application/grpc" &&
                _ignoreGrpcRequests)
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