namespace ChuckDeviceControllerPluginMvc1.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Plugin;

    /// <summary>
    /// Set the [ApiController] attribute when the controller is used as a backend
    /// API and no user interface is needed.
    /// Details: <see href="https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-6.0#apicontroller-attribute"/>
    /// 
    /// Set the [Produces(MediaTypeNAmes.Application.Json)] attribute to determine
    /// the format of the response data.
    /// 
    /// Inherit the controller class using 'Controller' when Mvc View support is
    /// needed and a user interface will be shown.
    /// Inherit the controller class using 'ControllerBase' when Mvc View support is
    /// not needed and no Views will be used. (i.e. backend web API)
    /// </summary>
    // [Produces(MediaTypeNames.Application.Json)]
    // [ApiController]
    // [Route("[controller]")]
    // Sets the default base route for all methods in the class
    // to the '[controller]' placeholder, in this case 'Test'.
    [Authorize(Roles = RoleConsts.DefaultRole)]
    public class TestController : Controller // ControllerBase, Controller
    {
        private readonly IDatabaseHost _databaseHost;

        public TestController(IDatabaseHost databaseHost)
        {
            _databaseHost = databaseHost;

            var device = _databaseHost.GetByIdAsync<IDevice, string>("SGV7SE").ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine($"Device: {device}");
        }

        /// <summary>
        /// GET: TestController i.e. '/Test/Index' route.
        /// </summary>
        /// <returns>Returns an action result for the route.</returns>
        public ActionResult Index()
        {
            // Render default 'Index.cshtml' view in `Views/Test` views folder.
            return View();
        }

        // GET: TestController/Details/test
        public ActionResult Details()
        {
            // TODO: Add example using view models
            return View();
        }
    }
}