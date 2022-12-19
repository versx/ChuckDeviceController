namespace ChuckDeviceController.PluginManager.Mvc.Razor;

using Microsoft.AspNetCore.Mvc.Razor;

public class PluginViewLocationExpander : IViewLocationExpander
{
    private const string ViewsFolderName = "Views";
    private const string ControllerActionPathFormat = "/{1}/{0}.cshtml";

    private readonly string _rootPluginsDirectory;
    private readonly IEnumerable<string> _pluginFolderNames;

    public PluginViewLocationExpander(string rootPluginsDirectory, IEnumerable<string> pluginFolderNames)
    {
        _rootPluginsDirectory = rootPluginsDirectory;
        _pluginFolderNames = pluginFolderNames;
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var locations = viewLocations
            .Where(loc => !string.IsNullOrEmpty(loc))
            .ToList();

        foreach (var pluginFolderName in _pluginFolderNames)
        {
            var pluginViewsFormat = BuildPluginViewsFormat(_rootPluginsDirectory, pluginFolderName);
            if (!locations.Contains(pluginViewsFormat))
            {
                locations.Add(pluginViewsFormat);
            }
        }
        return locations;
    }

    public void PopulateValues(ViewLocationExpanderContext context)
    {
    }

    private static string BuildPluginViewsFormat(string rootFolder, string pluginFolderName)
    {
        var pluginViewsFolder = Path.Join(rootFolder, pluginFolderName, ViewsFolderName);
        var pluginViewsFormat = Path.Join(pluginViewsFolder, ControllerActionPathFormat);
        var path = pluginViewsFormat.Replace('\\', '/');
        // i.e. ~/Views/Shared/Plugins/<PluginFolderName>/{1}=ControllerName/{0}=ActionName.cshtml
        // i.e. ~/bin/debug/plugins/<PluginFolderName>/Views/{1}=ControllerName/{0}=ActionName.cshtml
        return path;
        //return Path.GetFullPath(path);
    }
}
