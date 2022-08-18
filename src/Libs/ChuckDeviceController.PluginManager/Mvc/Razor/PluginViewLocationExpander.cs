namespace ChuckDeviceController.PluginManager.Mvc.Razor
{
    using Microsoft.AspNetCore.Mvc.Razor;

    public class PluginViewLocationExpander : IViewLocationExpander
    {
        private const string ViewsFolderName = "Views";
        private const string ControllerActionPathFormat = "/{1}/{0}.cshtml";

        private readonly string _rootPluginsDirectory;
        private readonly IEnumerable<string> _pluginFolderNames;
        private readonly IEnumerable<string> _viewsSearchDirectories;

        public PluginViewLocationExpander(string rootPluginsDirectory, IEnumerable<string> pluginFolderNames)
        {
            _rootPluginsDirectory = rootPluginsDirectory;
            _pluginFolderNames = pluginFolderNames;

            var baseSearchDir = _rootPluginsDirectory[2..]; // removes ./
            _viewsSearchDirectories = new[]
            {
                $"{baseSearchDir}",
                $"/{baseSearchDir}",
                $"~/{baseSearchDir}",
                $"./{baseSearchDir}",
                $"../{baseSearchDir}",
            };
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var locations = viewLocations.Where(loc => !string.IsNullOrEmpty(loc))
                                         .ToList();

            foreach (var pluginFolderName in _pluginFolderNames)
            {
                foreach (var baseSearchDir in _viewsSearchDirectories)
                {
                    var pluginViewsFormat = BuildPluginViewsFormat(baseSearchDir, pluginFolderName);
                    if (!locations.Contains(pluginViewsFormat))
                    {
                        locations.Add(pluginViewsFormat);
                    }
                    Console.WriteLine($"pluginViewsFormat: {pluginViewsFormat}");
                }
            }
            return locations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            Console.WriteLine($"Context: {context}");
        }

        private static string BuildPluginViewsFormat(string rootFolder, string pluginFolderName)
        {
            var pluginViewsFolder = Path.Join(rootFolder, pluginFolderName, ViewsFolderName);
            var pluginViewsFormat = Path.Join(pluginViewsFolder, ControllerActionPathFormat);
            var path = pluginViewsFormat.Replace('\\', '/');
            // i.e. ~/bin/debug/plugins/<PluginFolderName>/Views/{1}=ControllerName/{0}=ActionName.cshtml
            return path;
            //return Path.GetFullPath(path);
        }
    }
}
