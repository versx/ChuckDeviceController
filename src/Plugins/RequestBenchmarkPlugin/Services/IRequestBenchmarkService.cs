namespace RequestBenchmarkPlugin.Services
{
    using Utilities;

    public interface IRequestBenchmarkService
    {
        IReadOnlyDictionary<string, RequestBenchmark> Benchmarks { get; }

        void UpdateRouteBenchmark(string route, RequestBenchmark benchmark);

        void Delete(string route);

        void ClearBenchmarks();
    }
}