namespace TestPlugin.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Plugins;

    //[ApiController]
    [Route("[controller]")]
    public class TestController : Controller // ApiController, ControllerBase, Controller
    {
        private readonly IPluginService _testService;

        public TestController(IPluginService testService)
        {
            _testService = testService;
            Console.WriteLine($"TestService: {_testService.Test}");
        }

        // GET: TestController
        public ActionResult Index()
        {
            return View();
        }
    }
}