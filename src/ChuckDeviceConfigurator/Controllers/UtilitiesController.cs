namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize(Roles = RoleConsts.UtilitiesRole)]
    public class UtilitiesController : Controller
    {
        // GET: UtilitiesController
        public ActionResult Index()
        {
            return View();
        }
    }
}
