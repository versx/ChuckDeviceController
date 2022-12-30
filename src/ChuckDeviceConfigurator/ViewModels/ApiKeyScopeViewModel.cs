namespace ChuckDeviceConfigurator.ViewModels;

using ChuckDeviceController.Data.Common;

public class ApiKeyScopeViewModel
{
    public string GroupName { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public PluginApiKeyScope Value { get; set; }

    public bool Selected { get; set; }
}