namespace ChuckDeviceController.PluginManager.Services.Loader;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

using ChuckDeviceController.PluginManager.Services.Loader.Dependencies;
using ChuckDeviceController.PluginManager.Services.Loader.Runtime;
using ChuckDeviceController.PluginManager.Services.Loader.Runtime.Platform;

public class PluginDependencyContextProvider : IPluginDependencyContextProvider
{
    private const string DependenciesExtension = ".deps.json";
    private const string LibsPath = "lib/";
    private const string RefsPath = "refs";
    private const string VersionSuffix = ",Version=v";
    private const string NetStandardSuffix = ".netstandard";
    private const string ReferenceAssembly = "referenceassembly";

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
            LoadAssemblyAndReferencesFromCurrentAppDomain(
                type.Assembly.GetName(),
                hostDependencies,
                loadContext.DowngradableHostTypes,
                loadContext.DowngradableHostAssemblies
            );
        }

        foreach (var assemblyFileName in loadContext.HostAssemblies)
        {
            // Load host types from current app domain
            LoadAssemblyAndReferencesFromCurrentAppDomain(
                assemblyFileName,
                hostDependencies,
                loadContext.DowngradableHostTypes,
                loadContext.DowngradableHostAssemblies
            );
        }

        // Check plugin assembly target framework against host target framework
        var dependencyContext = GetDependencyContext(loadContext.AssemblyFullPath);
        var pluginFramework = dependencyContext?.Target?.Framework ?? string.Empty;
        CheckFrameworkCompatibility(loadContext.HostFramework, pluginFramework, loadContext.IgnorePlatformInconsistencies);

        var pluginDependencies = GetPluginDependencies(dependencyContext);
        var resourceDependencies = GetResourceDependencies(dependencyContext);
        var platformDependencies = GetPlatformDependencies(dependencyContext, loadContext.PluginPlatformVersion);
        var remoteDependencies = GetRemoteDependencies(loadContext);
        var pluginReferenceDependencies = GetPluginReferenceDependencies(dependencyContext);

        var pluginDependencyContext = new PluginDependencyContext
        (
            loadContext.AssemblyFullPath,
            hostDependencies,
            remoteDependencies,
            pluginDependencies,
            resourceDependencies,
            pluginReferenceDependencies,
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
        var depFileName = Path.GetFileNameWithoutExtension(assemblyFullPath) + DependenciesExtension;
        var depFilePath = Path.Combine(depFolder!, depFileName);
        if (!File.Exists(depFilePath))
        {
            _logger.LogWarning($"Plugin assembly dependencies file '{depFileName}' does not exist, unable to determine or load all possible dependencies.");
            return null;
        }

        using var fs = File.OpenRead(depFilePath);
        return new DependencyContextJsonReader().Read(fs);
    }

    private static void CheckFrameworkCompatibility(string hostFramework, string pluginFramework, bool ignorePlatformInconsistencies)
    {
        // NOTE: Add conditional to ignore compatibility check when adding migrations
        if (hostFramework == ".NETCoreApp,Version=v2.0" && pluginFramework == ".NETCoreApp,Version=v7.0" && !ignorePlatformInconsistencies)
        {
            // Migration, return to skip compatibility check
            _logger.LogDebug($"Host: {hostFramework}, Plugin: {pluginFramework}");
            return;
        }

        if (ignorePlatformInconsistencies)
            return;

        // Check if the host and plugin frameworks match
        if (pluginFramework == hostFramework)
            return;

        _logger.LogWarning($"Plugin framework {pluginFramework} does not match host framework {hostFramework}");

        var pluginFrameworkSplit = pluginFramework.Split(new[] { VersionSuffix }, StringSplitOptions.RemoveEmptyEntries);
        var hostFrameworkSplit = hostFramework.Split(new[] { VersionSuffix }, StringSplitOptions.RemoveEmptyEntries);

        if (pluginFrameworkSplit.Length != hostFrameworkSplit.Length)
        {
            throw new Exception($"Framework target parts for plugin and host do not match. Likely unable to find 'plugin.deps.json' file to load dependencies.");
        }

        var pluginFrameworkType = pluginFrameworkSplit[0];
        var hostFrameworkType = hostFrameworkSplit[0];
        if (pluginFrameworkType.ToLower() == NetStandardSuffix)
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

        // Check if the major version of the plugin is higher
        if (pluginFrameworkVersionMajor > hostFrameworkVersionMajor ||
            // Or if the major version is the same but the minor version is higher
            (pluginFrameworkVersionMajor == hostFrameworkVersionMajor && pluginFrameworkVersionMinor > hostFrameworkVersionMinor))
        {
            throw new Exception($"Plugin framework version {pluginFramework} is newer than the Host {hostFramework}. Please upgrade the Host to load this Plugin.");
        }
    }

    private static void LoadAssemblyAndReferencesFromCurrentAppDomain(AssemblyName assemblyName, List<HostDependency> hostDependencies, IEnumerable<Type> downgradableHostTypes, IEnumerable<string> downgradableAssemblies)
    {
        if (assemblyName?.Name == null || hostDependencies.Any(h => h.DependencyName.Name == assemblyName.Name))
            return;

        var allowDowngrade =
            downgradableHostTypes.Any(type => type.Assembly.GetName().Name == assemblyName.Name) ||
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
        LoadAssemblyAndReferencesFromCurrentAppDomain(assemblyName, hostDependencies, downgradableHostTypes, downgradableAssemblies);
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
        var dependencyGraph = DependencyContext.Default?.RuntimeGraph.FirstOrDefault(graph => graph.Runtime == runtimeId);
        if (dependencyGraph == null)
        {
            return dependencies;
        }
        // List of supported runtimes, includes the default runtime and the fallbacks for this dependency context
        var runtimes = new List<string> { dependencyGraph.Runtime };
        runtimes.AddRange(dependencyGraph.Fallbacks!);

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
                var path = asset.StartsWith(LibsPath)
                    ? Path.GetFileName(asset)
                    : asset;

                dependencies.Add(new PluginDependency
                {
                    DependencyNameWithoutExtension = Path.GetFileNameWithoutExtension(asset),
                    //Version = new Version(runtimeLibrary.Version),
                    Version = runtimeLibrary.Version,
                    DependencyPath = path,
                    ProbingPath = Path.Combine(runtimeLibrary.Name.ToLowerInvariant(), runtimeLibrary.Version, path),
                });
            }
        }
        return dependencies;
    }

    private static IEnumerable<PluginDependency> GetPluginReferenceDependencies(DependencyContext? pluginDependencyContext)
    {
        var dependencies = new List<PluginDependency>();
        if (pluginDependencyContext == null)
        {
            return dependencies;
        }

        var referenceAssemblies = pluginDependencyContext.CompileLibraries.Where(r => r.Type == ReferenceAssembly);
        foreach (var referenceAssembly in referenceAssemblies)
        {
            foreach (var assembly in referenceAssembly.Assemblies)
            {
                dependencies.Add(new PluginDependency
                {
                    DependencyNameWithoutExtension = Path.GetFileNameWithoutExtension(assembly),
                    //Version = new Version(referenceAssembly.Version),
                    Version = referenceAssembly.Version,
                    DependencyPath = Path.Join(RefsPath, assembly),
                });
            }
        }
        return dependencies;
    }

    private static IEnumerable<PlatformDependency> GetPlatformDependencies(DependencyContext? pluginDependencyContext, PluginPlatformVersion pluginPlatformVersion)
    {
        var dependencies = new List<PlatformDependency>();
        if (pluginDependencyContext == null)
        {
            return dependencies;
        }

        var runtimeId = GetCorrectRuntimeIdentifier();
        var dependencyGraph = DependencyContext.Default?.RuntimeGraph.FirstOrDefault(graph => graph.Runtime == runtimeId);
        if (dependencyGraph == null)
        {
            return dependencies;
        }
        // List of supported runtimes, includes the default runtime and the fallbacks for this dependency context
        var runtimes = new List<string> { dependencyGraph.Runtime };
        runtimes.AddRange(dependencyGraph.Fallbacks!);

        var runtimePlatformContext = new RuntimePlatformContext();
        var platformExtensions = runtimePlatformContext.GetPlatformExtensions();
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
                var platformDependencyPath = ResolvePlatformDependencyPathToRuntime(pluginPlatformVersion, asset);
                dependencies.Add(new PlatformDependency
                {
                    DependencyNameWithoutExtension = Path.GetFileNameWithoutExtension(platformDependencyPath),
                    Version = new Version(runtimeLibrary.Version),
                    DependencyPath = platformDependencyPath,
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

        var runtimeLibraries = pluginDependencyContext.RuntimeLibraries.Where(lib => lib.ResourceAssemblies?.Any() ?? false);
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

    private static string ResolvePlatformDependencyPathToRuntime(PluginPlatformVersion pluginPlatformVersion, string platformDependencyPath)
    {
        var runtimePlatformContext = new RuntimePlatformContext();
        var runtimeInformation = runtimePlatformContext.GetRuntimeInfo();
        var runtimes = runtimeInformation.Runtimes;
        if (pluginPlatformVersion.IsSpecified)
        {
            // First filter on specific version
            runtimes = runtimes.Where(r => r.Version == pluginPlatformVersion.Version);
            // Then, filter on target runtime, this is not always provided
            if (pluginPlatformVersion.Runtime != RuntimeType.None)
            {
                runtimes = runtimes.Where(r => r.RuntimeType == pluginPlatformVersion.Runtime);
            }

            if (!runtimes.Any())
            {
                throw new Exception($"Requested runtime platform is not installed {pluginPlatformVersion.Runtime} {pluginPlatformVersion.Version}");
            }
        }

        foreach (var runtime in runtimes.OrderByDescending(r => r.Version))
        {
            var platformDependencyName = Path.GetFileName(platformDependencyPath);
            var platformDependencyFileVersion = FileVersionInfo.GetVersionInfo(platformDependencyName);
            var platformFiles = Directory.GetFiles(runtime.Location);
            var candidateFilePath = platformFiles.FirstOrDefault(f => string.Compare(Path.GetFileName(f), platformDependencyName) == 0);
            if (!string.IsNullOrEmpty(candidateFilePath))
            {
                var candidateFileVersion = FileVersionInfo.GetVersionInfo(candidateFilePath);
                if (string.Compare(platformDependencyFileVersion.FileVersion, candidateFileVersion.FileVersion) == 0)
                {
                    return candidateFilePath;
                }
            }
        }

        return string.Empty;
    }

    #endregion
}