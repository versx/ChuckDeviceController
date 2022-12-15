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
            var json = new JsonResult(ProtoDataStatistics.Instance);
            return await Task.FromResult(json);
        }

        [HttpGet("Reset")]
        public ActionResult ResetStatistics()
        {
            ProtoDataStatistics.Instance.Reset();
            return Ok();
        }
    }
}