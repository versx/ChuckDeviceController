namespace ChuckDeviceConfigurator.Localization;

using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;

using ChuckDeviceController.Extensions.Json;

public class JsonStringLocalizer : IStringLocalizer
{
    private readonly IDistributedCache _cache;
    private readonly string _localesBasePath;
    private readonly Dictionary<string, string> _manifest = new();
    private static readonly Dictionary<string, string> _missing = new();

    public bool LogMissingKeys { get; set; }

    public JsonStringLocalizer(IDistributedCache cache, IConfiguration configuration)
    {
        LogMissingKeys = true;

        _cache = cache;
        _localesBasePath = $"{Strings.BasePath}/{Strings.LocaleFolder}";

        var locale = configuration["locale"];
        var filePath = $"{_localesBasePath}/{locale}.json";

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
        foreach (var (key, value) in _manifest)
        {
            var cacheKey = $"locale_{locale}_{key}";
            _cache.SetString(cacheKey, value);
        }
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name, value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var actualValue = this[name];
            return !actualValue.ResourceNotFound
                ? new LocalizedString(name, string.Format(actualValue.Value, arguments), false)
                : actualValue;
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        // TODO: Get locale from config
        var currentLocale = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
        var filePath = $"{_localesBasePath}/{currentLocale}.json";
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
        foreach (var (key, value) in manifest!)
        {
            yield return new LocalizedString(key, value, false);
        }
    }

    private string GetString(string key)
    {
        var currentLocale = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
        var cacheKey = $"locale_{currentLocale}_{key}";
        var cacheValue = _cache.GetString(cacheKey);
        // If cache value is not null, return it
        if (!string.IsNullOrEmpty(cacheValue))
        {
            return cacheValue;
        }

        // Attempt to get value from localization dictionary
        if (!_manifest.TryGetValue(key, out var result))
        {
            // Value not in cache
            if (LogMissingKeys && !_missing.ContainsKey(key))
            {
                _missing.Add(key, key);
                File.WriteAllText($"missing_locales.txt", _missing.ToJson(pretty: true));
            }
            return key;
        }

        // If value is not null, set in cache
        if (!string.IsNullOrEmpty(result))
        {
            _cache.SetString(cacheKey, result);
        }

        return result;
    }
}

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;

    public JsonStringLocalizerFactory(IDistributedCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _configuration = configuration;
    }

    public IStringLocalizer Create(Type resourceSource) =>
        new JsonStringLocalizer(_cache, _configuration);

    public IStringLocalizer Create(string baseName, string location) =>
        new JsonStringLocalizer(_cache, _configuration);
}