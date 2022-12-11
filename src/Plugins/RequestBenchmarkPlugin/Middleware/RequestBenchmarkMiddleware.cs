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
        //private readonly object _lock = new();
        private readonly SemaphoreSlim _sem = new(1, 1);

        public RequestBenchmarkMiddleware(RequestDelegate next, IRequestBenchmarkService benchmarkService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _benchmarkService = benchmarkService ?? throw new ArgumentNullException(nameof(benchmarkService));
        }

        public async Task InvokeAsync(HttpContext context, RequestTimesDbContext dbContext)
        {
            await _sem.WaitAsync();

            var route = context.Request.Path.ToString();
            var exists = _benchmarkService.Benchmarks.ContainsKey(route);
            var benchmark = exists
                ? _benchmarkService.Benchmarks[route]
                : new(route);

            using (StopwatchTimer.Initialize(benchmark))
            {
                await _next(context);
            }

            var timing = RequestTime.FromRequestBenchmark(benchmark);
            if (exists)
            {
                dbContext.Update(timing);
            }
            else
            {
                await dbContext.AddAsync(timing);
            }

            try { await dbContext.SaveChangesAsync(); } catch { }
            _benchmarkService.UpdateRouteBenchmark(route, benchmark);

            _sem.Release();
        }
    }
}