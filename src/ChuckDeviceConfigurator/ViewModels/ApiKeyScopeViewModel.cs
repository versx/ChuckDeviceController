namespace ChuckDeviceConfigurator.ViewModels
{
    using ChuckDeviceController.Common.Data.Contracts;

    public class ApiKeyScopeViewModel
    {
        public PluginApiKeyScope Scope { get; set; }

        public bool Selected { get; set; }
    }
}