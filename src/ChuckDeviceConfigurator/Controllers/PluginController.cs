﻿namespace ChuckDeviceConfigurator.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.PluginManager;

[Authorize(Roles = RoleConsts.PluginsRole)]
public class PluginController : Controller
{
    private readonly ILogger<PluginController> _logger;
    private readonly IUiHost _uiHost;
    private readonly IUnitOfWork _uow;

    public PluginController(
        ILogger<PluginController> logger,
        IUiHost uiHost,
        IUnitOfWork uow)
    {
        _logger = logger;
        _uiHost = uiHost;
        _uow = uow;
    }

    // GET: PluginController
    public async Task<ActionResult> Index()
    {
        var plugins = PluginManager.Instance.Plugins.Values.ToList();
        var apiKeys = await _uow.ApiKeys.FindAllAsync();
        var model = new PluginsViewModel
        {
            Plugins = plugins,
            ApiKeys = apiKeys.ToList(),
        };
        return View(model);
    }

    // GET: PluginController/Details/5
    public ActionResult Details(string id)
    {
        if (!PluginManager.Instance.Plugins.ContainsKey(id))
        {
            ModelState.AddModelError("Plugin", $"Plugin with name '{id}' has not been loaded or registered.");
            return View();
        }
        var plugin = PluginManager.Instance.Plugins[id];
        return View(plugin);
    }

    // GET: PluginController/Reload/5
    public async Task<ActionResult> Reload(string id)
    {
        if (!PluginManager.Instance.Plugins.ContainsKey(id))
        {
            ModelState.AddModelError("Plugin", $"Plugin with name '{id}' has not been loaded or registered.");
            return View();
        }

        await PluginManager.Instance.ReloadAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // GET: PluginController/Manage/5
    public async Task<ActionResult> Manage(string id)
    {
        if (!PluginManager.Instance.Plugins.ContainsKey(id))
        {
            ModelState.AddModelError("Plugin", $"Plugin with name '{id}' has not been loaded or registered yet.");
            return View();
        }

        var pluginHost = PluginManager.Instance.Plugins[id];
        var state = pluginHost.State != PluginState.Running ? PluginState.Running : PluginState.Disabled;
        if (state == PluginState.Disabled)
        {
            await PluginManager.Instance.StopAsync(id);

            // TODO: Remove any UI elements registered by plugin if state == Disabled
            //await pluginHost.HostHandlers.UiHost.RemoveUiElementsAsync(id);
        }
        await PluginManager.Instance.SetStateAsync(id, state);

        _logger.LogInformation($"[{id}] Plugin has been {(pluginHost.State == PluginState.Running ? "enabled" : "disabled")}");
        return RedirectToAction(nameof(Index));
    }
}