namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    public class ManageApiKeyViewModel
    {
        [DisplayName("ID")]
        public uint Id { get; set; }

        [DisplayName("Name")]
        public string? Name { get; set; }

        [DisplayName("API Key")]
        public string? Key { get; set; }

        [DisplayName("Expiration Date")]
        public DateTime Expiration { get; set; }

        [DisplayName("Scope")]
        public List<ApiKeyScopeViewModel> Scope { get; set; } = new();

        [DisplayName("Enabled")]
        public bool IsEnabled { get; set; }
    }
}