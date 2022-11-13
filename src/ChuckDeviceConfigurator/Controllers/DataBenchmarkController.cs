﻿namespace ChuckDeviceConfigurator.Controllers
{
    using System.ComponentModel;
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
        // GET: DataBenchmarkController
        public async Task<ActionResult> Index()
        {
            // TODO: Add controller endpoint to config
            var data = await NetUtils.GetAsync("http://localhost:8882/api/Statistics");
            var model = data?.FromJson<ProtoDataStatistics>() ?? new();
            return View(model);
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
        public DataEntityTime AverageTime { get; set; } = new();
    }

    public class DataEntityTime : IComparable<DataEntityTime>
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("count")]
        [DisplayName("Count")]
        public ulong Count { get; set; }

        [JsonPropertyName("time_s")]
        [DisplayName("Time (sec)")]
        public double TimeS { get; set; }

        public int CompareTo(DataEntityTime? other)
        {
            if (other == null)
                return -1;

            if (other.Id != Id)
                return -1;

            return 0;
        }
    }
}