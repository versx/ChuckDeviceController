namespace ChuckDeviceConfigurator.Services.Plugins
{
    public interface IApiKeyManagerService
    {
        Task<bool> ValidateKey(string apiKey);

        Task InvalidateKey(string apiKey);

        string GenerateApiKey();
    }
}