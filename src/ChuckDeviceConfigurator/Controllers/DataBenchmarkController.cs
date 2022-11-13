namespace ChuckDeviceConfigurator.Controllers
{
    using System.Text.Json.Serialization;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Net.Utilities;

    [Controller]
    [Authorize(Roles = RoleConsts.BenchmarksRole)]
    public class DataBenchmarkController : Controller
    {
        // TODO: Add controller endpoint to config
        private const string StatsEndpoint = "http://localhost:8882/api/Statistics";

        // GET: DataBenchmarkController
        public async Task<ActionResult> Index()
        {
            var data = await NetUtils.GetAsync(StatsEndpoint);
            var model = data?.FromJson<ProtoDataStatistics>() ?? new();
            return View(model);
        }

        // GET: DataBenchmarkController/Clear
        public async Task<ActionResult> Reset()
        {
            _ = await NetUtils.GetAsync(StatsEndpoint + "/Reset");
            return RedirectToAction(nameof(Index));
        }
    }

    // TODO: Move to shared library
    public class ProtoDataStatistics
    {
        [JsonPropertyName("total_requests")]
        public ulong TotalRequestsProcessed { get; set; }

        [JsonPropertyName("protos_received")]
        public uint TotalProtoPayloadsReceived { get; set; }

        [JsonPropertyName("protos_processed")]
        public uint TotalProtosProcessed { get; set; }

        [JsonPropertyName("entities_processed")]
        public uint TotalEntitiesProcessed { get; set; }

        [JsonPropertyName("entities_upserted")]
        public uint TotalEntitiesUpserted { get; set; }

        [JsonPropertyName("data_benchmark_times")]
        public IReadOnlyList<DataEntityTime> Times { get; set; } = Array.Empty<DataEntityTime>();

        [JsonPropertyName("avg_benchmark_time")]
        public DataEntityTime? AverageTime { get; set; }
    }
}