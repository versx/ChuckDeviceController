namespace ChuckDeviceConfigurator.Services.Plugins
{
    using System.Security.Cryptography;

    using ChuckDeviceController.Data.Contexts;

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

        public async Task<bool> ValidateKey(string apiKey)
        {
            var exists = _context.ApiKeys.Any(key => key.Equals(apiKey));
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