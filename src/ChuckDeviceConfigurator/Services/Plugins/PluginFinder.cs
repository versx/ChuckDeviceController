﻿namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Reflection;

    using ChuckDeviceController.Plugins;

    public class PluginFinder<TPlugin> where TPlugin : class, IPlugin
    {
        private const string PluginSearchPattern = "*.dll";
        private const string AssemblyContextName = "PluginFinderAssemblyContext";

        private readonly string _pluginsFolder;

        #region Constructor

        public PluginFinder(string pluginsFolder)
        {
            _pluginsFolder = pluginsFolder;
        }

        #endregion

        #region Public Methods

        public IReadOnlyCollection<string> FindAssemliesWithPlugins()
        {
            var options = new EnumerationOptions
            {
                MaxRecursionDepth = 2,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false,
            };
            var assemblies = Directory.GetFiles(_pluginsFolder, PluginSearchPattern, options);
            return FindPluginsInAssemblies(assemblies);
        }

        public static IReadOnlyCollection<Type> GetPluginTypes(Assembly assembly)
        {
            var types = assembly.GetTypes()
                                .Where(type => typeof(TPlugin).IsAssignableFrom(type))
                                .Where(type => type.IsClass && !type.IsAbstract)
                                .ToList();
            return types;
        }

        #endregion

        #region Private Methods

        private static IReadOnlyCollection<string> FindPluginsInAssemblies(string[] assemblyPaths)
        {
            var pluginPaths = new List<string>();
            var pluginFinderAssemblyContext = new PluginAssemblyLoadContext(AssemblyContextName);
            foreach (var assemblyPath in assemblyPaths)
            {
                var assemblyFullPath = Path.GetFullPath(assemblyPath);
                var assembly = pluginFinderAssemblyContext.LoadFromAssemblyPath(assemblyFullPath);
                if (GetPluginTypes(assembly).Any())
                {
                    pluginPaths.Add(assembly.Location);
                }
            }
            pluginFinderAssemblyContext.Unload();
            return pluginPaths;
        }

        #endregion
    }
}