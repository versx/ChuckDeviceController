namespace ChuckDeviceController.PluginManager.Services.Loader
{
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;

    using Microsoft.Extensions.DependencyModel;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.PluginManager.Services.Loader.Dependencies;
    using ChuckDeviceController.PluginManager.Services.Loader.Runtime.Platform;

    public class PluginDependencyContextProvider : IPluginDependencyContextProvider
    {
        #region Variables

        private static readonly ILogger<IPluginDependencyContextProvider> _logger =
            new Logger<IPluginDependencyContextProvider>(LoggerFactory.Create(x => x.AddConsole()));

        #endregion

        #region Public Methods

        public IPluginDependencyContext LoadFromPluginLoadContext(IPluginAssemblyLoadContext loadContext)
        {
            var hostDependencies = new List<HostDependency>();

            foreach (var type in loadContext.HostTypes)
            {
                // Load host types from current app domain
                LoadAssemblyAndReferencesFromCurrentAppDomain(type.Assembly.GetName(), hostDependencies, loadContext.DowngradableHostTypes, loadContext.DowngradableHostAssemblies);
            }

            foreach (var assemblyFileName in loadContext.HostAssemblies)
            {
                // Load host types from current app domain
                LoadAssemblyAndReferencesFromCurrentAppDomain(assemblyFileName, hostDependencies, loadContext.DowngradableHostTypes, loadContext.DowngradableHostAssemblies);
            }

            // Check plugin assembly target framework against host target framework
            var dependencyContext = GetDependencyContext(loadContext.AssemblyFullPath);
            var pluginFramework = dependencyContext?.Target.Framework;
            CheckFrameworkCompatibility(loadContext.HostFramework, pluginFramework, loadContext.IgnorePlatformInconsistencies);

            var pluginDependencies = GetPluginDependencies(dependencyContext);
            var resourceDependencies = GetResourceDependencies(dependencyContext);
            var platformDependencies = GetPlatformDependencies(dependencyContext);
            var remoteDependencies = GetRemoteDependencies(loadContext);

            var pluginDependencyContext = new PluginDependencyContext
            (
                loadContext.AssemblyFullPath,
                hostDependencies,
                remoteDependencies,
                pluginDependencies,
                resourceDependencies,
                platformDependencies,
                loadContext.AdditionalProbingPaths
            );
            return pluginDependencyContext;
        }

        #endregion

        #region Private Methods

        private static DependencyContext? GetDependencyContext(string assemblyFullPath)
        {
            var depFolder = Path.GetDirectoryName(assemblyFullPath);
            var depFileName = Path.GetFileNameWithoutExtension(assemblyFullPath) + ".deps.json";
            var depFilePath = Path.Combine(depFolder, depFileName );
            if (!File.Exists(depFilePath))
            {
                _logger.LogWarning($"Plugin assembly dependencies file '{depFileName}' does not exist, unable to determine or load all possible dependencies.");
                return null;
            }

            using (var fs = File.OpenRead(depFilePath))
            {
                return new DependencyContextJsonReader().Read(fs);
            }
        }

        private static void CheckFrameworkCompatibility(string hostFramework, string pluginFramework, bool ignorePlatformInconsistencies)
        {
            if (ignorePlatformInconsistencies)
                return;

            // Check if the host and plugin frameworks match
            if (pluginFramework == hostFramework)
                return;

            _logger.LogWarning($"Plugin framework {pluginFramework} does not match host framework {hostFramework}");

            var pluginFrameworkSplit = pluginFramework.Split(new[] { ",Version=v" }, StringSplitOptions.RemoveEmptyEntries);
            var hostFrameworkSplit = hostFramework.Split(new[] { ",Version=v" }, StringSplitOptions.RemoveEmptyEntries);

            var pluginFrameworkType = pluginFrameworkSplit[0];
            var hostFrameworkType = hostFrameworkSplit[0];
            if (pluginFrameworkType.ToLower() == ".netstandard")
            {
                throw new Exception($"Plugin framework {pluginFramework} might have compatibility issues with the host {hostFramework}, use the IgnorePlatformInconsistencies flag to skip this check.");
            }

            if (pluginFrameworkType != hostFrameworkType)
            {
                throw new Exception($"Plugin framework {pluginFramework} does not match the host {hostFramework}. Please target {hostFramework} in order to load the plugin.");
            }

            var pluginFrameworkVersion = pluginFrameworkSplit[1];
            var hostFrameworkVersion = hostFrameworkSplit[1];
            var pluginSplit = pluginFrameworkVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var hostSplit = hostFrameworkVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var pluginFrameworkVersionMajor = int.Parse(pluginSplit[0]);
            var pluginFrameworkVersionMinor = int.Parse(pluginSplit[1]);
            var hostFrameworkVersionMajor = int.Parse(hostSplit[0]);
            var hostFrameworkVersionMinor = int.Parse(hostSplit[1]);

            // If the major version of the plugin is higher
            if (pluginFrameworkVersionMajor > hostFrameworkVersionMajor ||
                // Or the major version is the same but the minor version is higher
                (pluginFrameworkVersionMajor == hostFrameworkVersionMajor && pluginFrameworkVersionMinor > hostFrameworkVersionMinor))
            {
                throw new Exception($"Plugin framework version {pluginFramework} is newer than the Host {hostFramework}. Please upgrade the Host to load this Plugin.");
            }
        }

        private static void LoadAssemblyAndReferencesFromCurrentAppDomain(AssemblyName assemblyName, List<HostDependency> hostDependencies, IEnumerable<Type> downgradableHostTypes, IEnumerable<string> downgradableAssemblies)
        {
            if (assemblyName?.Name == null || hostDependencies.Any(h => h.DependencyName.Name == assemblyName.Name))
                return;

            var allowDowngrade = downgradableHostTypes.Any(type => type.Assembly.GetName().Name == assemblyName.Name) ||
                                 downgradableAssemblies.Any(asm => asm == assemblyName.Name);
            hostDependencies.Add(new HostDependency
            {
                DependencyName = assemblyName,
                AllowDowngrade = allowDowngrade,
            });

            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    LoadAssemblyAndReferencesFromCurrentAppDomain(reference, hostDependencies, downgradableHostTypes, downgradableAssemblies);
                }
            }
            catch (FileNotFoundException)
            {
                // Should only occur when the assembly is a platform assembly
                _logger.LogWarning($"Failed to load assembly '{assemblyName.Name}', likely a platform assembly.");
            }
        }

        private static void LoadAssemblyAndReferencesFromCurrentAppDomain(string assemblyFileName, List<HostDependency> hostDependencies, IEnumerable<Type> downgradableHostTypes, IEnumerable<string> downgradableAssemblies)
        {
            var assemblyName = new AssemblyName(assemblyFileName);
            if (assemblyFileName == null || hostDependencies.Any(h => h.DependencyName.Name == assemblyName.Name))
                return;

            var allowDowngrade = downgradableHostTypes.Any(type => type.Assembly.GetName().Name == assemblyName.Name) ||
                                downgradableAssemblies.Any(asm => asm == assemblyName.Name);
            hostDependencies.Add(new HostDependency
            {
                DependencyName = assemblyName,
                AllowDowngrade = allowDowngrade,
            });

            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    LoadAssemblyAndReferencesFromCurrentAppDomain(reference, hostDependencies, downgradableHostTypes, downgradableAssemblies);
                }
            }
            catch (FileNotFoundException)
            {
                // Should only occur when the assembly is a platform assembly
                _logger.LogWarning($"Failed to load assembly '{assemblyName.Name}', likely a platform assembly.");
            }
        }

        #region GetDependencies

        private static IEnumerable<PluginDependency> GetPluginDependencies(DependencyContext? pluginDependencyContext)
        {
            var dependencies = new List<PluginDependency>();
            if (pluginDependencyContext == null)
            {
                return dependencies;
            }

            var runtimeId = GetCorrectRuntimeIdentifier();
            var dependencyGraph = DependencyContext.Default.RuntimeGraph.FirstOrDefault(graph => graph.Runtime == runtimeId);
            // List of supported runtimes, includes the default runtime and the fallbacks for this dependency context
            var runtimes = new List<string> { dependencyGraph?.Runtime };
            if (dependencyGraph != null)
            {
                runtimes.AddRange(dependencyGraph.Fallbacks);
            }

            foreach (var runtimeLibrary in pluginDependencyContext.RuntimeLibraries)
            {
                var assets = runtimeLibrary.RuntimeAssemblyGroups.GetDefaultAssets();
                foreach (var runtime in runtimes)
                {
                    var runtimeSpecificGroup = runtimeLibrary.RuntimeAssemblyGroups.FirstOrDefault(group => group.Runtime == runtime);
                    if (runtimeSpecificGroup != null)
                    {
                        assets = runtimeSpecificGroup.AssetPaths;
                        break;
                    }
                }
                foreach (var asset in assets)
                {
                    var path = asset.StartsWith("lib/")
                        ? Path.GetFileName(asset)
                        : asset;

                    dependencies.Add(new PluginDependency
                    {
                        DependencyNameWithoutExtension = Path.GetFileNameWithoutExtension(asset),
                        Version = new Version(runtimeLibrary.Version),
                        DependencyPath = path,
                        ProbingPath = Path.Combine(runtimeLibrary.Name.ToLowerInvariant(), runtimeLibrary.Version, path),
                    });
                }
            }
            return dependencies;
        }

        // Not currently used
        private static IEnumerable<PluginDependency> GetPluginReferenceDependencies(DependencyContext? pluginDependencyContext)
        {
            var dependencies = new List<PluginDependency>();
            if (pluginDependencyContext == null)
            {
                return dependencies;
            }

            var referenceAssemblies = pluginDependencyContext.CompileLibraries.Where(r => r.Type == "referenceassembly");
            foreach (var referenceAssembly in referenceAssemblies)
            {
                foreach (var assembly in referenceAssembly.Assemblies)
                {
                    dependencies.Add(new PluginDependency
                    {
                        DependencyNameWithoutExtension = Path.GetFileNameWithoutExtension(assembly),
                        Version = new Version(referenceAssembly.Version),
                        DependencyPath = Path.Join("refs", assembly),
                    });
                }
            }
            return dependencies;
        }

        private static IEnumerable<PlatformDependency> GetPlatformDependencies(DependencyContext? pluginDependencyContext)
        {
            var dependencies = new List<PlatformDependency>();
            if (pluginDependencyContext == null)
            {
                return dependencies;
            }

            var runtimeId = GetCorrectRuntimeIdentifier();
            var dependencyGraph = DependencyContext.Default.RuntimeGraph.FirstOrDefault(graph => graph.Runtime == runtimeId);
            // List of supported runtimes, includes the default runtime and the fallbacks for this dependency context
            var runtimes = new List<string> { dependencyGraph?.Runtime };
            if (dependencyGraph != null)
            {
                runtimes.AddRange(dependencyGraph.Fallbacks);
            }

            var platformExtensions = GetPlatformDependencyFileExtensions();
            foreach (var runtimeLibrary in pluginDependencyContext.RuntimeLibraries)
            {
                var assets = runtimeLibrary.NativeLibraryGroups.GetDefaultAssets();
                foreach (var runtime in runtimes)
                {
                    var runtimeSpecificGroup = runtimeLibrary.NativeLibraryGroups.FirstOrDefault(group => group.Runtime == runtime);
                    if (runtimeSpecificGroup != null)
                    {
                        assets = runtimeSpecificGroup.AssetPaths;
                        break;
                    }
                }

                // Only load assemblies and not debug files
                var validAssets = assets.Where(asset => platformExtensions.Contains(Path.GetExtension(asset)));
                foreach (var asset in validAssets)
                {
                    dependencies.Add(new PlatformDependency
                    {
                        DependencyNameWithoutExtension = Path.GetFileNameWithoutExtension(asset),
                        Version = new Version(runtimeLibrary.Version),
                        DependencyPath = asset,
                    });
                }
            }
            return dependencies;
        }

        private static IEnumerable<PluginResourceDependency> GetResourceDependencies(DependencyContext? pluginDependencyContext)
        {
            var dependencies = new List<PluginResourceDependency>();
            if (pluginDependencyContext == null)
            {
                return dependencies;
            }

            var runtimeLibraries = pluginDependencyContext.RuntimeLibraries
                .Where(lib => lib.ResourceAssemblies != null && lib.ResourceAssemblies.Any());
            foreach (var runtimeLibrary in runtimeLibraries)
            {
                dependencies.AddRange(runtimeLibrary.ResourceAssemblies
                    .Where(res => !string.IsNullOrEmpty(Path.GetDirectoryName(Path.GetDirectoryName(res.Path))))
                    .Select(res =>
                        new PluginResourceDependency
                        {
                            Path = Path.Combine(runtimeLibrary.Name.ToLowerInvariant(),
                                runtimeLibrary.Version,
                                res.Path)
                        })
                );
            }
            return dependencies;
        }

        private static IEnumerable<RemoteDependency> GetRemoteDependencies(IPluginAssemblyLoadContext pluginContext)
        {
            var dependencies = new List<RemoteDependency>();
            foreach (var type in pluginContext.RemoteTypes)
            {
                dependencies.Add(new RemoteDependency
                {
                    DependencyName = type.Assembly.GetName(),
                });
            }
            return dependencies;
        }

        #endregion

        private static string GetCorrectRuntimeIdentifier()
        {
            var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            if (PlatformAbstraction.IsMacOS() || PlatformAbstraction.IsWindows())
                return runtimeIdentifier;

            // Other: Linux, FreeBSD, ...
            return $"linux-{RuntimeInformation.ProcessArchitecture.ToString().ToLower()}";
        }

        private static string[] GetPlatformDependencyFileExtensions()
        {
            if (PlatformAbstraction.IsWindows())
                return new[] { ".dll" };
            if (PlatformAbstraction.IsMacOS())
                return new[] { ".dylib" };
            if (PlatformAbstraction.IsLinux())
                return new[] { ".so", ".so.1" };

            throw new Exception($"Platform {RuntimeInformation.OSDescription} is not supported");
        }

        #endregion
    }
}