namespace RobotsPlugin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceController.Plugin.Helpers.Attributes;

using Data.Abstractions;
using ViewModels;

[Authorize(Roles = RobotsPlugin.RobotsRole)]
[DenyRobot]
public class RouteController : Controller
{
    //public const string Name = "Robot";

    #region Variables

    private readonly IRobots _robots;

    #endregion

    #region Constructors

    public RouteController(IRobots robots)
    {
        _robots = robots ?? throw new ArgumentNullException(nameof(robots));
    }

    #endregion

    #region Controller Route Methods

    //[HttpGet]
    //public IActionResult Index()
    //{
    //    var customUserAgents = new List<CustomUserAgentViewModel>();
    //    foreach (var deniedRoute in _robots.DeniedRoutes)
    //    {
    //        customUserAgents.Add(new CustomUserAgentViewModel(
    //            deniedRoute.Id,
    //            deniedRoute.UserAgent,
    //            deniedRoute.Route
    //        ));
    //    }

    //    foreach (var customRoute in _robots.CustomRoutes)
    //    {
    //        customUserAgents.Add(new CustomUserAgentViewModel(
    //            customRoute.Id,
    //            customRoute.UserAgent,
    //            customRoute.Route,
    //            customRoute.Comment,
    //            customRoute.IsAllowed,
    //            isCustom: true
    //        ));
    //    }

    //    var model = new EditRobotsViewModel(
    //        _robots.UserAgents.ToList(),
    //        customUserAgents
    //    );
    //    return View(model);
    //}

    public ActionResult Details(string id)
    {
        var route = _robots.CustomRoutes.FirstOrDefault(route => route.Id.ToString() == id);
        if (route == null)
        {
            ModelState.AddModelError("Route", $"Failed to get route with id '{id}'");
            return View();
        }

        var model = new UserAgentRouteViewModel
        {
            UserAgent = route.UserAgent,
            Route = route.Route,
            Comment = route.Comment,
            IsAllowed = route.IsAllowed,
            IsCustom = route.IsCustom,
        };

        return View(model);
    }

    public ActionResult Create(string id)
    {
        if (!_robots.UserAgentExists(id))
        {
            ModelState.AddModelError("Route", $"User agent '{id}' does not exist.");
            return View();
        }

        var model = new UserAgentRouteViewModel
        {
            UserAgent = id,
            IsCustom = true,
        };

        return View(model);
    }

    [HttpPost]
    public ActionResult Create(string id, UserAgentRouteViewModel model)
    {
        if (model == null)
        {
            return RedirectToAction(nameof(Details), "Robot", new { id });
        }
        if (string.IsNullOrEmpty(model.UserAgent))
        {
            ModelState.AddModelError("Route", "Invalid UserAgent specified.");
            return View(model);
        }
        if (string.IsNullOrEmpty(model.Route))
        {
            ModelState.AddModelError("Route", "Invalid Route specified.");
            return View(model);
        }
        if (_robots.DeniedRouteExists(model.UserAgent, model.Route))
        {
            ModelState.AddModelError("Route", $"Denied Route already exists '{model.Route}' with UserAgent '{model.UserAgent}'");
            return View(model);
        }
        if (_robots.CustomRouteExists(model.UserAgent, model.Route))
        {
            ModelState.AddModelError("Route", $"Custom Route already exists '{model.Route}' with UserAgent '{model.UserAgent}'");
            return View(model);
        }
        //if (ModelState.ErrorCount > 0)
        ////if (!ModelState.IsValid)
        //{
        //    return View(model);
        //}
        if (!_robots.UserAgentExists(model.UserAgent))
        {
            _robots.AddUserAgent(model.UserAgent);
        }

        _robots.AddRoute(model.UserAgent, model.Route, isAllowed: model.IsAllowed, model.Comment);
        _robots.SaveData();

        return RedirectToAction(nameof(Details), "Robot", new { id });
    }

    public ActionResult Edit(string id)
    {
        // Get route data by UserAgent string
        var route = _robots.CustomRoutes.FirstOrDefault(cr => cr.Id.ToString() == id);
        if (route == null)
        {
            // Failed to find user agent
            return RedirectToAction(nameof(Details), "Robot", new { id });
        }

        var model = new UserAgentRouteViewModel
        {
            Id = route.Id,
            UserAgent = route.UserAgent,
            Route = route.Route,
            Comment = route.Comment,
            IsAllowed = route.IsAllowed,
            IsCustom = route.IsCustom,
        };
        return View(model);
    }

    [HttpPost]
    public ActionResult Edit(string id, UserAgentRouteViewModel model)
    {
        if (model == null)
        {
            return RedirectToAction(nameof(Details), "Robot", new { id = model?.UserAgent });
        }
        if (string.IsNullOrEmpty(model.UserAgent))
        {
            ModelState.AddModelError("Route", "Invalid UserAgent specified.");
        }
        if (string.IsNullOrEmpty(model.Route))
        {
            ModelState.AddModelError("Route", "Invalid Route specified.");
        }
        if (_robots.DeniedRouteExists(model.UserAgent, model.Route))
        {
            ModelState.AddModelError("Route", $"Route already exists '{model.Route}' with UserAgent '{model.UserAgent}'.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }
        if (!_robots.UserAgentExists(model.UserAgent))
        {
            // UserAgent string changed, update cache
            _robots.AddUserAgent(model.UserAgent);
        }

        _robots.UpdateRoute(model.UserAgent, id, model.Route, isAllowed: model.IsAllowed, model.Comment);
        _robots.SaveData();

        return RedirectToAction(nameof(Details), "Robot", new { id = model.UserAgent });
    }

    public ActionResult Delete(string id)
    {
        // Get route data by UserAgent string
        var route = _robots.CustomRoutes.FirstOrDefault(cr => cr.Id.ToString() == id);
        if (route == null)
        {
            // Failed to find user agent
            return RedirectToAction(nameof(Details), "Robot", new { id });
        }

        var model = new UserAgentRouteViewModel
        {
            Id = route.Id,
            UserAgent = route.UserAgent,
            Route = route.Route,
            Comment = route.Comment,
            IsAllowed = route.IsAllowed,
            IsCustom = route.IsCustom,
        };
        return View(model);
    }

    [HttpPost]
    public ActionResult Delete(string id, UserAgentRouteViewModel model)
    {
        if (string.IsNullOrEmpty(id))
        {
            ModelState.AddModelError("Route", "Invalid route id specified.");
            return View(model);
        }

        var route = _robots.CustomRoutes.FirstOrDefault(cr => cr.Id.ToString() == id);
        if (route == null)
        {
            ModelState.AddModelError("Route", $"Route not found with id '{id}'.");
            return View(model);
        }

        _robots.RemoveRoute(route.Id.ToString());
        _robots.SaveData();

        return RedirectToAction(nameof(Details), "Robot", new { id = model.UserAgent });
    }

    #endregion
}