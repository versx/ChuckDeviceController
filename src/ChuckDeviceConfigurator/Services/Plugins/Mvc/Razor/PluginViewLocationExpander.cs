namespace ChuckDeviceConfigurator.Services.Plugins.Mvc.Razor
{
    using Microsoft.AspNetCore.Mvc.Razor;

    public class PluginViewLocationExpander : IViewLocationExpander
    {
        private const string ViewsFolderName = "Views";
        private const string ControllerActionPathFormat = "/{1}/{0}.cshtml";

        private readonly string _rootPluginsDirectory;
        private readonly IEnumerable<string> _pluginFolderNames;

        public PluginViewLocationExpander(string rootPluginsDirectory, IEnumerable<string> pluginFolderNames)
        {
            _rootPluginsDirectory = rootPluginsDirectory.StartsWith(".")
                ? "~" + rootPluginsDirectory[1..]
                : rootPluginsDirectory.StartsWith("~")
                    ? rootPluginsDirectory
                    : "~" + rootPluginsDirectory;
            _pluginFolderNames = pluginFolderNames;
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var locations = viewLocations.Where(loc => !string.IsNullOrEmpty(loc))
                                         .ToList();

            foreach (var pluginFolderName in _pluginFolderNames)
            {
                var pluginViewsFolder = Path.Join(_rootPluginsDirectory, pluginFolderName, ViewsFolderName);
                var pluginViewsFormat = Path.Join(pluginViewsFolder, ControllerActionPathFormat);
                // i.e. ~/bin/debug/plugins/<PluginFolderName>/Views/{1}=ControllerName/{0}=ActionName.cshtml
                if (!locations.Contains(pluginViewsFormat))
                {
                    locations.Add(pluginViewsFormat);
                }
                Console.WriteLine($"pluginViewsFormat: {pluginViewsFormat}");
            }
            return locations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            Console.WriteLine($"Context: {context}");
        }
    }
}
