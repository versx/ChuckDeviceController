namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Plugins;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

    [Controller]
    [Authorize(Roles = RoleConsts.ApiKeysRole)]
    public class ApiKeyController : Controller
    {
        private readonly ILogger<ApiKeyController> _logger;
        private readonly ControllerDbContext _context;
        private readonly IApiKeyManagerService _apiKeyManager;

        public ApiKeyController(
            ILogger<ApiKeyController> logger,
            ControllerDbContext context,
            IApiKeyManagerService apiKeyManager)
        {
            _logger = logger;
            _context = context;
            _apiKeyManager = apiKeyManager;
        }

        // GET: ApiKeyController
        public ActionResult Index()
        {
            var apiKeys = _context.ApiKeys.ToList();
            var model = new ViewModelsModel<ApiKey>
            {
                Items = apiKeys,
            };
            return View(model);
        }

        // GET: ApiKeyController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ApiKeyController/Create
        public ActionResult Create()
        {
            var scopes = ((PluginApiKeyScope[])Enum.GetValues(typeof(PluginApiKeyScope)))
                .Where(scope => scope != PluginApiKeyScope.None)
                .Select(scope => new ApiKeyScopeViewModel { Scope = scope, Selected = false })
                .ToList();
            var model = new CreateApiKeyViewModel
            {
                Scope = scopes,
            };
            return View(model);
        }

        // POST: ApiKeyController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateApiKeyViewModel model)
        {
            try
            {
                var scope = model.Scope
                    .Where(scope => scope.Selected)
                    .Select(scope => scope.Scope)
                    .ToList();
                var seconds = model.Expiration.Ticks == 0
                    ? 0
                    : model.Expiration.ToTotalSeconds();
                var pluginApiKey = _apiKeyManager.GenerateApiKey();
                var apiKey = new ApiKey
                {
                    Name = model.Name,
                    Key = pluginApiKey,
                    ExpirationTimestamp = seconds,
                    Scope = scope,
                    IsEnabled = model.IsEnabled,
                };
                await _context.ApiKeys.AddAsync(apiKey);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("ApiKey", $"Unknown error occurred while creating new API key.");
                return View(model);
            }
        }

        // GET: ApiKeyController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ApiKeyController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ApiKeyController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ApiKeyController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}