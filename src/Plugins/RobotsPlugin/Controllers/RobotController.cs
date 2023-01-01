namespace RobotsPlugin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceController.Extensions;
using ChuckDeviceController.Plugin.Helpers.Attributes;

using Data.Abstractions;
using ViewModels;

[Authorize(Roles = RobotsPlugin.RobotsRole)]
[DenyRobot]
public class RobotController : Controller
{
    private readonly IRobots _robots;

    public RobotController(IRobots robots)
    {
        _robots = robots ?? throw new ArgumentNullException(nameof(robots));
    }

    // GET: RobotController
    public ActionResult Index()
    {
        var model = _robots.UserAgents
            .ToDictionary(u => u, u => _robots.CustomRoutes.Where(r => r.UserAgent == u).ToList())
            .Select(x => new UserAgentViewModel(x.Key, (uint)x.Value.Count))
            .ToList();
        return View(model);
    }

    // GET: RobotController/Details/Mozilla...
    public ActionResult Details(string id)
    {
        var routes = _robots.CustomRoutes
            .Where(route => route.UserAgent == id)
            .ToList();
        var model = new UserAgentRoutesViewModel
        {
            UserAgent = id,
            Routes = routes,
        };

        return View(model);
    }

    // GET: RobotController/Create
    public ActionResult Create()
    {
        return View();
    }

    // POST: RobotController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(CreateUserAgentViewModel model)
    {
        try
        {
            if (_robots.UserAgentExists(model.UserAgent))
            {
                ModelState.AddModelError("Robot", $"User agent already exists '{model.UserAgent}'");
                return View(model);
            }

            _robots.AddUserAgent(model.UserAgent);
            _robots.SaveData();

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Robot", $"Unknown error occurred while creating user agent '{model.UserAgent}'.");
            return View(model);
        }
    }

    // GET: RobotController/Delete/Mozilla...
    public ActionResult Delete(string id)
    {
        var model = new CreateUserAgentViewModel
        {
            UserAgent = id,
            // TODO: Include routes for user agent
        };

        return View(model);
    }

    // POST: RobotController/Delete/Mozilla...
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(string id, CreateUserAgentViewModel model)
    {
        try
        {
            if (!_robots.UserAgentExists(model.UserAgent))
            {
                ModelState.AddModelError("Robot", $"User agent does not exist '{model.UserAgent}'");
                return View(model);
            }

            _robots.RemoveUserAgent(id);
            _robots.SaveData();

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Robot", $"Unknown error occurred while deleting user agent '{id}'.");
            return View(model);
        }
    }

    // GET: RobotController/DenyAll
    public ActionResult DenyAll()
    {
        var wildcard = "*";
        if (!_robots.UserAgentExists(wildcard))
        {
            _robots.AddUserAgent(wildcard);
        }
        if (!_robots.CustomRouteExists(wildcard, "/"))
        {
            _robots.AddRoute(wildcard, "/", isAllowed: false, "Deny all web crawlers");
        }
        _robots.SaveData();

        return RedirectToAction(nameof(Index));
    }
}