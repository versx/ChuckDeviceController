namespace RequestBenchmarkPlugin.Services;

using System.Collections.Concurrent;

using Data.Contexts;
using Data.Entities;
using Utilities;

public class RequestBenchmarkService : IRequestBenchmarkService
{
    private static readonly ConcurrentDictionary<string, RequestBenchmark> _requestBenchmarks = new();
    private static readonly object _lock = new();
    private static readonly SemaphoreSlim _sem = new(1, 1);
    private readonly ILogger<IRequestBenchmarkService> _logger;

    public IReadOnlyDictionary<string, RequestBenchmark> Benchmarks => _requestBenchmarks;

    public RequestBenchmarkService(ILogger<IRequestBenchmarkService> logger)
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
            if (!_requestBenchmarks.TryRemove(route, out var _))
            {
                _logger.LogWarning("Failed to remove request benchmark for route '{Route}' from cache", route);
                return;
            }
            _logger.LogDebug("Removed benchmark for route '{Route}' from benchmark cache", route);
        }
    }

    public void UpdateRouteBenchmark(string route, RequestBenchmark benchmark)
    {
        lock (_lock)
        {
            _requestBenchmarks.AddOrUpdate(route, benchmark, (key, oldValue) => benchmark);
        }
    }

    public async Task SaveRouteBenchmark(RequestTimesDbContext context, string route, RequestTime timing)
    {
        await _sem.WaitAsync();

        if (context.RequestTimes.Any(x => x.Route == route))
        {
            context.Update(timing);
        }
        else
        {
            await context.AddAsync(timing);
        }

        try { await context.SaveChangesAsync(); }
        catch (Exception ex)
        {
            _logger.LogError("Error: {Message}", ex.InnerException?.Message ?? ex.Message);
        }

        _sem.Release();
    }
}