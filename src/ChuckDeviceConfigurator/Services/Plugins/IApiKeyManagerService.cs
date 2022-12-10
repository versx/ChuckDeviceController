namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Data.Entities;

    public interface IApiKeyManagerService
    {
        Task<string> GetApiKey(uint id);

        Task<string> GetApiKey(string name);

        Task<ApiKey> GetApiKeyByName(string name);

        Task<bool> ValidateKey(string apiKey);

        Task InvalidateKey(string apiKey);

        string GenerateApiKey();
    }
}