namespace RequestBenchmarkPlugin.Services
{
    using System.Collections.Concurrent;

    using Utilities;

    public class RequestBenchmarkService : IRequestBenchmarkService
    {
        private static readonly ConcurrentDictionary<string, RequestBenchmark> _requestBenchmarks = new();
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
                if (!_requestBenchmarks.ContainsKey(route))
                    return;

                if (!_requestBenchmarks.TryRemove(route, out var _))
                {
                    _logger.LogWarning($"Failed to remove request benchmark for route '{route}' from cache");
                    return;
                }
                _logger.LogDebug($"Removed benchmark for route '{route}' from benchmark cache");
            }
        }

        public void UpdateRouteBenchmark(string route, RequestBenchmark benchmark)
        {
            lock (_lock)
            {
                if (_requestBenchmarks.ContainsKey(route))
                {
                    _requestBenchmarks[route] = benchmark;
                    return;
                }

                if (!_requestBenchmarks.TryAdd(route, benchmark))
                {
                    _logger.LogWarning($"Failed to add request benchmark for route '{route}' to cache");
                }
            }
        }
    }
}