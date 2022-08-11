namespace TestPlugin.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Plugins;

    /// <summary>
    /// Set the [ApiController] attribute when the controller is used as a backend
    /// API and no user interface is needed.
    /// Details: <see href="https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-6.0#apicontroller-attribute"/>
    /// 
    /// Set the [Produces(MediaTypeNAmes.Application.Json)] attribute to determine
    /// the format of the response data.
    /// 
    /// Inherit the controller class using 'Controller' when Mvc View support is needed.
    /// Inherit the controller class using 'ControllerBase' when Mvc View support is
    /// not needed and no Views will be used. (i.e. backend web API)
    /// </summary>
    // [Produces(MediaTypeNames.Application.Json)]
    // [ApiController]
    [Route("[controller]")]
    // Sets the default base route for all methods in the class
    // to the '[controller]' placeholder, in this case 'Test'.
    public class TestController : Controller // ControllerBase, Controller
    {
        private readonly IPluginService _testService;

        public TestController(IPluginService testService)
        {
            _testService = testService;
            Console.WriteLine($"TestService: {_testService.Test}");
        }

        /// <summary>
        /// GET: TestController i.e. '/Test/Index' route.
        /// </summary>
        /// <returns>Returns an action result for the route.</returns>
        public ActionResult Index()
        {
            // Render default 'Index' view in `Views/Test` views folder.
            return View();
        }
    }
}