namespace ChuckDeviceController.PluginManager.Services.Finder
{
    using System.Reflection;

    public interface IAssemblyShim
    {
        Assembly Assembly { get; }

        string AssemblyFullPath { get; }

        IEnumerable<Type> Types { get; }
    }

    public class PluginAssembly : IAssemblyShim
    {
        public Assembly Assembly { get; }

        public string AssemblyFullPath => Assembly.Location;

        public IEnumerable<Type> Types => Assembly?.GetTypes() ?? Enumerable.Empty<Type>();

        public PluginAssembly(Assembly assembly)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }
    }
}