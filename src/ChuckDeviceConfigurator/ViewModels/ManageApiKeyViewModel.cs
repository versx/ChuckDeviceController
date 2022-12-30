namespace ChuckDeviceConfigurator.ViewModels;

using System.ComponentModel;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.PluginManager;

public class ManageApiKeyViewModel
{
    [DisplayName("ID")]
    public uint Id { get; set; }

    [DisplayName("Name")]
    public string Name { get; set; } = null!;

    [DisplayName("API Key")]
    public string Key { get; set; } = null!;

    [DisplayName("Expiration Date")]
    public ulong? Expiration { get; set; } = 0;

    [DisplayName("Scopes")]
    public PluginApiKeyScope Scope { get; set; } = PluginApiKeyScope.None;

    [DisplayName("Scopes")]
    public Dictionary<string, List<ApiKeyScopeViewModel>> Scopes { get; set; } = new();

    [DisplayName("Enabled")]
    public bool IsEnabled { get; set; }

    [DisplayName("Plugins")]
    public IReadOnlyList<IPluginHost> Plugins { get; set; } = new List<IPluginHost>();
}