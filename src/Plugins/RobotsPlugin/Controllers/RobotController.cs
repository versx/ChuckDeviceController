namespace RobotsPlugin.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using Data.Contracts;
    using ViewModels;

    using ChuckDeviceController.Plugin.Helpers.Attributes;

    [Authorize(Roles = RobotsPlugin.RobotsRole)]
    [DenyRobot]
    public class RobotController : Controller
    {
        //public const string Name = "Robot";

        #region Variables

        private readonly IRobots _robots;

        #endregion

        #region Constructors

        public RobotController(IRobots robots)
        {
            _robots = robots ?? throw new ArgumentNullException(nameof(robots));
        }

        #endregion

        #region Controller Route Methods

        [HttpGet]
        public IActionResult Index()
        {
            var customUserAgents = new List<CustomUserAgentViewModel>();
            foreach (var deniedRoute in _robots.DeniedRoutes)
            {
                customUserAgents.Add(new CustomUserAgentViewModel(deniedRoute.UserAgent, deniedRoute.Route));
            }

            foreach (var customRoute in _robots.CustomRoutes)
            {
                customUserAgents.Add(new CustomUserAgentViewModel(
                    customRoute.UserAgent,
                    customRoute.Route,
                    customRoute.Comment,
                    customRoute.IsAllowed,
                    isCustom: true
                ));
            }

            var model = new EditRobotsViewModel(
                _robots.UserAgents.ToList(),
                customUserAgents
            );
            return View(model);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(CustomUserAgentViewModel model)
        {
            if (model == null)
            {
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrEmpty(model.UserAgent))
            {
                ModelState.AddModelError("Robot", "Invalid UserAgent specified.");
            }
            if (string.IsNullOrEmpty(model.Route))
            {
                ModelState.AddModelError("Robot", "Invalid Route specified.");
            }
            if (DeniedRouteExists(model.UserAgent, model.Route))
            {
                ModelState.AddModelError("Robot", $"Denied Route already exists '{model.Route}' with UserAgent '{model.UserAgent}'");
            }
            if (CustomRouteExists(model.UserAgent, model.Route))
            {
                ModelState.AddModelError("Robot", $"Custom Route already exists '{model.Route}' with UserAgent '{model.UserAgent}'");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (!UserAgentExists(model.UserAgent))
            {
                _robots.AddUserAgent(model.UserAgent);
            }

            _robots.AddRoute(model.UserAgent, model.Route, isAllowed: model.IsAllowed, model.Comment);
            _robots.SaveData();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string id)
        {
            // Get route data by UserAgent string
            var route = _robots.CustomRoutes.FirstOrDefault(cr => IsEqual(cr.UserAgent, id));
            if (route == null)
            {
                // Failed to find user agent
                return RedirectToAction(nameof(Index));
            }

            var model = new CustomUserAgentViewModel
            {
                UserAgent = route.UserAgent,
                Route = route.Route,
                Comment = route.Comment,
                IsAllowed = route.IsAllowed,
                IsCustom = route.IsCustom,
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(string id, CustomUserAgentViewModel model)
        {
            if (model == null)
            {
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrEmpty(model.UserAgent))
            {
                ModelState.AddModelError("Robot", "Invalid UserAgent specified.");
            }
            if (string.IsNullOrEmpty(model.Route))
            {
                ModelState.AddModelError("Robot", "Invalid Route specified.");
            }
            if (DeniedRouteExists(model.UserAgent, model.Route))
            {
                ModelState.AddModelError("Robot", $"Route already exists '{model.Route}' with UserAgent '{model.UserAgent}'.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (!UserAgentExists(model.UserAgent))
            {
                // UserAgent string changed, update cache
                _robots.AddUserAgent(model.UserAgent);
            }

            _robots.UpdateRoute(model.UserAgent, model.Route, isAllowed: model.IsAllowed, model.Comment);
            _robots.SaveData();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string id)
        {
            // Get route data by UserAgent string
            var route = _robots.CustomRoutes.FirstOrDefault(cr => IsEqual(cr.UserAgent, id));
            if (route == null)
            {
                // Failed to find user agent
                return RedirectToAction(nameof(Index));
            }

            var model = new CustomUserAgentViewModel
            {
                UserAgent = route.UserAgent,
                Route = route.Route,
                Comment = route.Comment,
                IsAllowed = route.IsAllowed,
                IsCustom = route.IsCustom,
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Delete(string id, string route)
        {
            if (string.IsNullOrEmpty(id))
            {
                ModelState.AddModelError("Robot", "Invalid UserAgent specified.");
                return View();
            }

            if (string.IsNullOrEmpty(route))
            {
                ModelState.AddModelError("Robot", "Invalid Route specified.");
                return View();
            }

            var customRoute = _robots.CustomRoutes.FirstOrDefault(cr =>
                IsEqual(cr.UserAgent, id) &&
                //IsEqual(cr.Route, model.Route) &&
                cr.IsCustom
            );

            if (customRoute == null)
            {
                ModelState.AddModelError("Robot", "Custom route not found with UserAgent '{id}'.");
            }
            else
            {
                _robots.RemoveRoute(customRoute.UserAgent, customRoute.Route);
                _robots.SaveData();
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Private Methods

        private bool DeniedRouteExists(string userAgent, string route)
        {
            var exists = _robots.DeniedRoutes.Any(dr => IsEqual(dr.UserAgent, userAgent) && IsEqual(dr.Route, route));
            return exists;
        }

        private bool CustomRouteExists(string userAgent, string route)
        {
            var exists = _robots.CustomRoutes.Any(cr => IsEqual(cr.UserAgent, userAgent) && IsEqual(cr.Route, route));
            return exists;
        }

        private bool UserAgentExists(string userAgent)
        {
            var exists = _robots.UserAgents.Any(a => IsEqual(a, userAgent));
            return exists;
        }

        private static bool IsEqual(string a, string b)
        {
            return a.Equals(b, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion
    }
}