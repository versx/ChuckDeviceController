namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Security.Cryptography;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class ApiKeyManagerService : IApiKeyManagerService
    {
        #region Constants

        private const string Prefix = "CDC-";
        private const int NumberOfSecureBytesToGenerate = 32;
        private const int LengthOfKey = 36;

        #endregion

        #region Variables

        private readonly ControllerDbContext _context;

        #endregion

        #region Constructor

        public ApiKeyManagerService(ControllerDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Public Methods

        public async Task<string> GetApiKey(uint id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            return apiKey?.Key;
        }

        public async Task<string> GetApiKey(string name)
        {
            var apiKey = _context.ApiKeys.FirstOrDefault(key => key.Name == name);
            return await Task.FromResult(apiKey?.Key);
        }

        public async Task<ApiKey> GetApiKeyByName(string name)
        {
            var apiKey = _context.ApiKeys.FirstOrDefault(key => key.Name == name);
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
            var exists = _context.ApiKeys.Any(key => key.Equals(apiKey) && key.IsEnabled);

            return await Task.FromResult(exists);
        }

        public async Task InvalidateKey(string apiKey)
        {
            var entity = _context.ApiKeys.FirstOrDefault(key => key.Equals(apiKey));
            if (entity == null)
            {
                // TODO: Error unable to find key
                return;
            }

            // TODO: Remove or disable API key
            _context.ApiKeys.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(NumberOfSecureBytesToGenerate);
            var apiKey = string.Concat(Prefix, Convert.ToBase64String(bytes)
                .Replace("/", "")
                .Replace("+", "")
                .Replace("=", "")
                .AsSpan(0, LengthOfKey - Prefix.Length));
            return apiKey;
        }

        #endregion
    }
}