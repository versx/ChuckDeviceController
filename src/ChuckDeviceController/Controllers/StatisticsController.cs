namespace ChuckDeviceController.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var json = new JsonResult(new
            {
                total_requests = ProtoDataStatistics.Instance.TotalRequestsProcessed,
                protos_received = ProtoDataStatistics.Instance.TotalProtoPayloadsReceived,
                protos_processed = ProtoDataStatistics.Instance.TotalProtosProcessed,
                entities_processed = ProtoDataStatistics.Instance.TotalEntitiesProcessed,
                entities_upserted = ProtoDataStatistics.Instance.TotalEntitiesUpserted,
                avg_benchmark_time = ProtoDataStatistics.Instance.AverageTime,
                data_benchmark_times = ProtoDataStatistics.Instance.Times,
                total_data_benchmark_times = ProtoDataStatistics.Instance.Times.Count,
            });
            return await Task.FromResult(json);
        }
    }
}