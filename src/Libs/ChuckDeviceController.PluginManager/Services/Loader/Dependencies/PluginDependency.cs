namespace ChuckDeviceController.PluginManager.Services.Loader.Dependencies
{
    public class PluginDependency
    {
        public string DependencyNameWithoutExtension { get; set; }

        public Version Version { get; set; }

        public string DependencyPath { get; set; }

        public string ProbingPath { get; set; }
    }
}