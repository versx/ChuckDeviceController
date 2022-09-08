namespace MemoryBenchmarkPlugin.Controllers
{
    using Microsoft.AspNetCore.Mvc;

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