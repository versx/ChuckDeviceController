namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    using ChuckDeviceController.Common.Data.Contracts;

    public class CreateApiKeyViewModel
    {
        [DisplayName("Expiration Date")]
        public DateTime Expiration { get; set; }

        [DisplayName("Scope")]
        public List<ApiKeyScopeViewModel> Scope { get; set; } = new(); // string / ApiKeyScopeViewModel

        [DisplayName("Enabled")]
        public bool IsEnabled { get; set; }
    }

    public class ApiKeyScopeViewModel
    {
        public PluginApiKeyScope Scope { get; set; }

        public bool Selected { get; set; }
    }
}