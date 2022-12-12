namespace RequestBenchmarkPlugin.Services
{
    using Data.Contexts;
    using Data.Entities;
    using Utilities;

    public interface IRequestBenchmarkService
    {
        IReadOnlyDictionary<string, RequestBenchmark> Benchmarks { get; }

        Task SaveRouteBenchmark(RequestTimesDbContext context, string route, RequestTime timing);

        void UpdateRouteBenchmark(string route, RequestBenchmark benchmark);

        void Delete(string route);

        void ClearBenchmarks();
    }
}