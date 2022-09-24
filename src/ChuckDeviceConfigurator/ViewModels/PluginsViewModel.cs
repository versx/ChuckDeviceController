namespace ChuckDeviceConfigurator.ViewModels
{
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.PluginManager;

    public class PluginsViewModel
    {
        public IReadOnlyList<IPluginHost> Plugins { get; set; } = new List<IPluginHost>();

        public IReadOnlyList<IApiKey> ApiKeys { get; set; } = new List<IApiKey>();
    }
}