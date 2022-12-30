namespace RequestBenchmarkPlugin;

public class RequestBenchmarkConfig
{
    public bool IgnoreGrpcRequests { get; set; }

    public List<string> IgnoredCustomRoutes { get; set; } = new();
}