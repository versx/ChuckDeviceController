namespace ChuckDeviceConfigurator.Services.Plugins;

using System.Security.Cryptography;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;

public class ApiKeyManagerService : IApiKeyManagerService
{
    #region Constants

    private const string Prefix = "CDC-";
    private const int NumberOfSecureBytesToGenerate = 32;
    private const int LengthOfKey = 36;

    #endregion

    #region Variables

    private readonly ILogger<IApiKeyManagerService> _logger;
    private readonly IUnitOfWork _uow;

    #endregion

    #region Constructor

    public ApiKeyManagerService(
        ILogger<IApiKeyManagerService> logger,
        IUnitOfWork uow)
    {
        _logger = logger;
        _uow = uow;
    }

    #endregion

    #region Public Methods

    public async Task<string> GetApiKey(uint id)
    {
        var apiKey = await _uow.ApiKeys.FindByIdAsync(id);
        return apiKey?.Key;
    }

    public async Task<string> GetApiKey(string name)
    {
        var apiKey = _uow.ApiKeys.FirstOrDefault(key => key.Name == name);
        return await Task.FromResult(apiKey?.Key);
    }

    public async Task<ApiKey> GetApiKeyByName(string name)
    {
        var apiKey = _uow.ApiKeys.FirstOrDefault(key => key.Name == name);
        return await Task.FromResult(apiKey);
    }

    public async Task<bool> ValidateKey(string apiKey)
    {
        // Validate key value is set
        if (string.IsNullOrEmpty(apiKey))
            return false;

        // Validate key length/character count
        if (apiKey.Length != LengthOfKey)
            return false;

        // Validate key starts with prefix
        if (!apiKey.StartsWith(Prefix))
            return false;

        // Validate key exists in database and enabled
        var exists = _uow.ApiKeys.Any(key => key.Equals(apiKey) && key.IsEnabled);

        return await Task.FromResult(exists);
    }

    public async Task InvalidateKey(string apiKey)
    {
        var entity = _uow.ApiKeys.FirstOrDefault(key => key.Key!.Equals(apiKey));
        if (entity == null)
        {
            _logger.LogError($"Unable to validate API key '{apiKey}', it does not exist.");
            return;
        }

        _uow.ApiKeys.Remove(entity);
        await _uow.CommitAsync();
    }

    public string GenerateApiKey()
    {
        // TODO: Replace with validatable method
        var bytes = RandomNumberGenerator.GetBytes(NumberOfSecureBytesToGenerate);
        var apiKey = string.Concat(Prefix, Convert.ToBase64String(bytes)
            .Replace("/", "")
            .Replace("+", "")
            .Replace("=", "")
            .AsSpan(0, LengthOfKey - Prefix.Length))
            .ToUpper();
        return apiKey;
    }

    #endregion
}