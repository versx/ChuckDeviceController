namespace HealthChecksPlugin.Controllers;

using System.Net;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using ChuckDeviceController.Extensions.Json;

[Route("/health")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly HealthCheckService _healthCheckService;

    public HealthController(
        ILogger<HealthController> logger,
        HealthCheckService healthCheckService)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    [Produces("application/json")]
    public async Task<IActionResult> Get()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        //var json = report.ToJson(pretty: true, converters: null);
        var json = JsonExtensions.ToJson(report, pretty: true, converters: null);

        _logger.LogInformation($"Get Health Information: {json}");

        var result = report.Status == HealthStatus.Healthy
            ? Ok(json)
            : StatusCode(
                (int)HttpStatusCode.ServiceUnavailable,
                json
            );
        return result;
    }
}