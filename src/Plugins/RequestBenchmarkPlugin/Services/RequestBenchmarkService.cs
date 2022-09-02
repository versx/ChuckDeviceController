namespace RequestBenchmarkPlugin.Services
{
    using Utilities;

    public class RequestBenchmarkService : IRequestBenchmarkService
    {
        private static readonly Dictionary<string, RequestBenchmark> _requestBenchmarks = new();
        private static readonly object _lock = new();
        private readonly ILogger<IRequestBenchmarkService> _logger;

        public IReadOnlyDictionary<string, RequestBenchmark> Benchmarks => _requestBenchmarks;

        public RequestBenchmarkService(
            ILogger<IRequestBenchmarkService> logger)
        {
            _logger = logger;
        }

        public void ClearBenchmarks()
        {
            lock (_lock)
            {
                _requestBenchmarks.Clear();
            }

            _logger.LogDebug($"Cleared all request benchmarks");
        }

        public void Delete(string route)
        {
            lock (_lock)
            {
                if (_requestBenchmarks.ContainsKey(route))
                {
                    _requestBenchmarks.Remove(route);
                    _logger.LogDebug($"Removed benchmark for route '{route}' from benchmark cache");
                }
            }
        }

        public void UpdateRouteBenchmark(string route, RequestBenchmark benchmark)
        {
            lock (_lock)
            {
                if (!_requestBenchmarks.ContainsKey(route))
                {
                    _requestBenchmarks.Add(route, benchmark);
                    return;
                }

                _requestBenchmarks[route] = benchmark;
            }
        }
    }
}