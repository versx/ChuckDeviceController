﻿namespace ChuckDeviceConfigurator.Controllers;

using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using ChuckDeviceController.Plugin;
using ChuckDeviceController.PluginManager;

public class SettingsController : Controller
{
    #region Constants

    private const string DefaultSettingsFileName = "settings.json";
    private const string DefaultSettingsFolderName = "";
    private const string DefaultCheckboxCheckedString = "checked";

    #endregion

    #region Variables

    private readonly IFileStorageHost _fileStorageHost;
    private readonly IUiHost _uiHost;

    #endregion

    public SettingsController(IFileStorageHost fileStorageHost, IUiHost uiHost)
    {
        _fileStorageHost = fileStorageHost;
        _uiHost = uiHost;
    }

    // GET: SettingsController
    public ActionResult Index()
    {
        var settings = LoadSettingsConfig();
        var model = new SettingsManager(settings);
        return View(model);
    }

    // POST: SettingsController
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Index(IFormCollection collection)
    {
        try
        {
            var settings = new Dictionary<string, object>();
            var keys = _uiHost.SettingsProperties.Values.SelectMany(x => x);
            foreach (var setting in keys)
            {
                var key = setting.Name;
                if (!collection.ContainsKey(key))
                {
                    var defaultValue = setting.Type == SettingsPropertyType.CheckBox
                        ? false
                        : setting.Value ?? setting.DefaultValue;
                    settings.Add(key, defaultValue!);
                    continue;
                }

                var value = FormatSettingValue(collection[key]);
                settings.Add(key, value);
            }
            SaveSettingsConfig(settings);

            // Convert settings values back to ViewModel happy values
            foreach (var key in settings.Keys)
            {
                settings[key] = FormatSettingValueFromConfig(settings[key]);
            }

            await Task.CompletedTask;
            return View(nameof(Index), new SettingsManager(settings));
        }
        catch
        {
            ModelState.AddModelError("Settings", $"Unknown error occurred while saving settings.");
            return View();
        }
    }

    private void SaveSettingsConfig(Dictionary<string, object> settings)
    {
        var result = _fileStorageHost.Save(settings, DefaultSettingsFolderName, DefaultSettingsFileName, prettyPrint: true);
        if (!result)
        {
            // Failed to save settings
            return;
        }

        foreach (var (_, pluginHost) in PluginManager.Instance.Plugins)
        {
            pluginHost.EventHandlers?.SettingsEvents?.OnSave(_uiHost.SettingsProperties);
        }
    }

    private Dictionary<string, object> LoadSettingsConfig()
    {
        var result = _fileStorageHost.Load<Dictionary<string, object>>(DefaultSettingsFolderName, DefaultSettingsFileName);
        var settings = new Dictionary<string, object>();
        foreach (var (key, value) in result)
        {
            settings.Add(key, FormatSettingValueFromConfig(value));
        }
        return settings;
    }

    private static object FormatSettingValue(object value)
    {
        if (value is StringValues checkbox && checkbox == "on")
        {
            return true;
        }
        return Convert.ToString(value) ?? string.Empty;
    }

    private static object FormatSettingValueFromConfig(object value)
    {
        if (value is bool checkbox)
        {
            return checkbox
                ? DefaultCheckboxCheckedString
                : string.Empty;
        }
        return Convert.ToString(value) ?? string.Empty;
    }
}

public class SettingsManager
{
    private readonly Dictionary<string, object> _settings = new();

    public SettingsManager(Dictionary<string, object> settings)
    {
        _settings = settings;
    }

    public T? Get<T>(string key, object? defaultValue = default)
    {
        if (!_settings.ContainsKey(key))
        {
            return defaultValue != null ? (T?)defaultValue : default;
        }

        try
        {
            var value = (T?)_settings[key];
            return value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            return (T?)defaultValue;
        }
    }
}