namespace ChuckDeviceConfigurator.PluginManager.Mvc.ApplicationParts
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc.ApplicationParts;

    internal sealed class PluginAssemblyPart : AssemblyPart, ICompilationReferencesProvider
    {
        public PluginAssemblyPart(Assembly assembly)
            : base(assembly)
        {
        }

        IEnumerable<string> ICompilationReferencesProvider.GetReferencePaths() => Enumerable.Empty<string>();
    }
}