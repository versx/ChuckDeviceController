namespace ChuckDeviceConfigurator.ViewModels;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.PluginManager;

public class PluginsViewModel
{
    public IReadOnlyList<IPluginHost> Plugins { get; set; } = new List<IPluginHost>();

    public IReadOnlyList<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}