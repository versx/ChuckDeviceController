namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Plugins;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Plugins;

    [Authorize(Roles = RoleConsts.PluginsRole)]
    public class PluginController : Controller
    {
        private readonly ILogger<PluginController> _logger;
        private readonly IPluginManager _pluginManager;
        private readonly IUiHost _uiHost;

        public PluginController(
            ILogger<PluginController> logger,
            IPluginManager pluginManager,
            IUiHost uiHost)
        {
            _logger = logger;
            _pluginManager = pluginManager;
            _uiHost = uiHost;
        }

        // GET: PluginController
        public ActionResult Index()
        {
            var plugins = _pluginManager.Plugins.Values.ToList();
            var model = new ViewModelsModel<PluginHost>
            {
                Items = plugins,
            };
            return View(model);
        }

        // GET: PluginController/Details/5
        public ActionResult Details(string id)
        {
            if (!_pluginManager.Plugins.ContainsKey(id))
            {
                ModelState.AddModelError("Plugin", $"Plugin with name '{id}' has not been loaded or registered.");
                return View();
            }
            var plugin = _pluginManager.Plugins[id];
            return View(plugin);
        }

        // GET: PluginController/Reload/5
        public ActionResult Reload(string id)
        {
            // TODO: Reload Plugin
            if (!_pluginManager.Plugins.ContainsKey(id))
            {
                ModelState.AddModelError("Plugin", $"Plugin with name '{id}' has not been loaded or registered.");
                return View();
            }
            //var plugin = _pluginManager.Plugins[id];
            //plugin.Reload();
            _logger.LogInformation($"Plugin '{id}' has been reloaded");

            return RedirectToAction(nameof(Index));
        }

        // GET: PluginController/Manage/5
        public ActionResult Manage(string id)
        {
            // TODO: Toggle - Enable/Disable Plugin
            if (!_pluginManager.Plugins.ContainsKey(id))
            {
                ModelState.AddModelError("Plugin", $"Plugin with name '{id}' has not been loaded or registered.");
                return View();
            }
            //var plugin = _pluginManager.Plugins[id];
            //plugin.SetState(); / ToggleState
            //var enabled = plugin.State == PluginState.Enabled;
            //_logger.LogInformation($"Plugin '{id}' has been '{(enabled ? "enabled" : "disabled")}");

            return RedirectToAction(nameof(Index));
        }

        // GET: PluginController/Upload
        public ActionResult Upload()
        {
            return View();
        }

        // POST: PluginController/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Upload(IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Name"]);
                if (_pluginManager.Plugins.ContainsKey(name))
                {
                    // Plugin already exists and is registered in plugin manager cache
                    ModelState.AddModelError("Plugin", $"Plugin already exists and is registered in plugin manager cache with name '{name}'.");
                    return View();
                }

                // TODO: Handle plugin upload, add to bin/plugins, move Views, etc

                await Task.CompletedTask;
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Plugin", $"Unknown error occurred while uploading plugin.");
                return View();
            }
        }

        public ActionResult GetPluginNavbarHeaders()
        {
            // Get cached navbar headers from plugins
            return new JsonResult(_uiHost.NavbarHeaders);
        }
    }
}