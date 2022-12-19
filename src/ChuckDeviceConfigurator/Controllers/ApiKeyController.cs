namespace ChuckDeviceConfigurator.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceConfigurator.Services.Plugins;
using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.PluginManager;

[Controller]
[Authorize(Roles = RoleConsts.ApiKeysRole)]
public class ApiKeyController : Controller
{
    private readonly ILogger<ApiKeyController> _logger;
    private readonly IUnitOfWork _uow;
    private readonly IApiKeyManagerService _apiKeyManager;

    public ApiKeyController(
        ILogger<ApiKeyController> logger,
        IUnitOfWork uow,
        IApiKeyManagerService apiKeyManager)
    {
        _logger = logger;
        _uow = uow;
        _apiKeyManager = apiKeyManager;
    }

    // GET: ApiKeyController/Details/5
    public async Task<ActionResult> Details(uint id)
    {
        var apiKey = await _uow.ApiKeys.FindByIdAsync(id);
        if (apiKey == null)
        {
            // Failed to retrieve API key from database, does it exist?
            ModelState.AddModelError("ApiKey", $"API key does not exist with id '{id}'.");
            return View();
        }
        var plugins = PluginManager.Instance.Plugins
            .Where(plugin => plugin.Value.ApiKey.Id == apiKey.Id)
            .Select(plugin => plugin.Value)
            .ToList();
        var scope = ((PluginApiKeyScope[])Enum.GetValues(typeof(PluginApiKeyScope)))
            .Where(scope => scope != PluginApiKeyScope.None)
            .Select(scope => new ApiKeyScopeViewModel { Scope = scope, Selected = apiKey.Scope?.Contains(scope) ?? false })
            .ToList();
        var model = new ManageApiKeyViewModel
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Key = apiKey.Key,
            Expiration = apiKey.ExpirationTimestamp,
            Scope = scope,
            IsEnabled = apiKey.IsEnabled,
            Plugins = plugins,
        };
        return View(model);
    }

    // GET: ApiKeyController/Create
    public ActionResult Create()
    {
        var scopes = ((PluginApiKeyScope[])Enum.GetValues(typeof(PluginApiKeyScope)))
            .Where(scope => scope != PluginApiKeyScope.None)
            .Select(scope => new ApiKeyScopeViewModel { Scope = scope, Selected = false })
            .ToList();
        var model = new ManageApiKeyViewModel
        {
            Scope = scopes,
        };
        return View(model);
    }

    // POST: ApiKeyController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(ManageApiKeyViewModel model)
    {
        try
        {
            if (string.IsNullOrEmpty(model?.Name))
            {
                ModelState.AddModelError("ApiKey", $"Failed to create API key, descriptive name is not set.");
                return View(model);
            }

            var scope = model.Scope
                .Where(scope => scope.Selected)
                .Select(scope => scope.Scope)
                .ToList();
            var seconds = model.Expiration == 0
                ? 0
                : model.Expiration;
            var pluginApiKey = _apiKeyManager.GenerateApiKey();
            var apiKey = new ApiKey
            {
                Name = model.Name,
                Key = pluginApiKey,
                ExpirationTimestamp = seconds,
                Scope = scope,
                IsEnabled = model.IsEnabled,
            };
            await _uow.ApiKeys.AddAsync(apiKey);
            await _uow.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("ApiKey", $"Unknown error occurred while creating new API key.");
            return View(model);
        }
    }

    // GET: ApiKeyController/Edit/5
    public async Task<ActionResult> Edit(uint id)
    {
        var apiKey = await _uow.ApiKeys.FindByIdAsync(id);
        if (apiKey == null)
        {
            // Failed to retrieve API key from database, does it exist?
            ModelState.AddModelError("ApiKey", $"API key does not exist with id '{id}'.");
            return View();
        }
        var scope = ((PluginApiKeyScope[])Enum.GetValues(typeof(PluginApiKeyScope)))
            .Where(scope => scope != PluginApiKeyScope.None)
            .Select(scope => new ApiKeyScopeViewModel { Scope = scope, Selected = apiKey.Scope?.Contains(scope) ?? false })
            .ToList();
        var model = new ManageApiKeyViewModel
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Key = apiKey.Key,
            Expiration = apiKey.ExpirationTimestamp,
            Scope = scope,
            IsEnabled = apiKey.IsEnabled,
        };
        return View(model);
    }

    // POST: ApiKeyController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(uint id, ManageApiKeyViewModel model)
    {
        try
        {
            var apiKey = await _uow.ApiKeys.FindByIdAsync(id);
            if (apiKey == null)
            {
                // Failed to retrieve API key from database, does it exist?
                ModelState.AddModelError("ApiKey", $"API key does not exist with id '{id}'.");
                return View(model);
            }
            var scope = model.Scope
                .Where(scope => scope.Selected)
                .Select(scope => scope.Scope)
                .ToList();
            var seconds = model.Expiration == 0
                ? 0
                : model.Expiration;

            apiKey.Name = model?.Name ?? string.Empty;
            apiKey.ExpirationTimestamp = seconds;
            apiKey.Scope = scope;
            apiKey.IsEnabled = model?.IsEnabled ?? false;

            await _uow.ApiKeys.UpdateAsync(apiKey);
            await _uow.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("ApiKey", $"Unknown error occurred while editing API key '{id}'.");
            return View(model);
        }
    }

    // GET: ApiKeyController/Delete/5
    public async Task<ActionResult> Delete(uint id)
    {
        var apiKey = await _uow.ApiKeys.FindByIdAsync(id);
        if (apiKey == null)
        {
            // Failed to retrieve API key from database, does it exist?
            ModelState.AddModelError("ApiKey", $"API key does not exist with id '{id}'.");
            return View();
        }
        var plugins = PluginManager.Instance.Plugins
            .Where(plugin => plugin.Value.ApiKey.Id == apiKey.Id)
            .Select(plugin => plugin.Value)
            .ToList();
        var scope = ((PluginApiKeyScope[])Enum.GetValues(typeof(PluginApiKeyScope)))
            .Where(scope => scope != PluginApiKeyScope.None)
            .Select(scope => new ApiKeyScopeViewModel { Scope = scope, Selected = apiKey.Scope?.Contains(scope) ?? false })
            .ToList();
        var model = new ManageApiKeyViewModel
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Key = apiKey.Key,
            Expiration = apiKey.ExpirationTimestamp,
            Scope = scope,
            IsEnabled = apiKey.IsEnabled,
            Plugins = plugins,
        };
        return View(model);
    }

    // POST: ApiKeyController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(uint id, ManageApiKeyViewModel model)
    {
        try
        {
            var apiKey = await _uow.ApiKeys.FindByIdAsync(id);
            if (apiKey == null)
            {
                // Failed to retrieve API key from database, does it exist?
                ModelState.AddModelError("ApiKey", $"API key does not exist with id '{id}'.");
                return View();
            }

            // Delete API key from database
            await _uow.ApiKeys.RemoveAsync(apiKey);
            await _uow.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("ApiKey", $"Unknown error occurred while deleting API key '{id}'.");
            return View();
        }
    }
}