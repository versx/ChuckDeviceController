namespace MemoryBenchmarkPlugin.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    // TODO: Add metric and event counters
    // Reference: https://docs.microsoft.com/en-us/dotnet/core/diagnostics/compare-metric-apis?view=aspnetcore-6.0

    public class MemoryController : Controller
    {
        private readonly ILogger<MemoryController> _logger;

        public MemoryController(ILogger<MemoryController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}