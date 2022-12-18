namespace MemoryBenchmarkPlugin.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    // Reference: https://docs.microsoft.com/en-us/dotnet/core/diagnostics/compare-metric-apis?view=aspnetcore-6.0

    [Authorize(Roles = MemoryBenchmarkPlugin.MemoryBenchmarkRole)]
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