namespace ChuckDeviceConfigurator.ViewModels;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.PluginManager;

public class PluginsViewModel
{
    public IReadOnlyList<IPluginHost> Plugins { get; set; } = new List<IPluginHost>();

    public IReadOnlyList<IApiKey> ApiKeys { get; set; } = new List<IApiKey>();
}