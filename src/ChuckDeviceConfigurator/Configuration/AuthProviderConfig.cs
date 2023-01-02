namespace ChuckDeviceConfigurator.Configuration;

using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Json;

public class AuthProviderConfig
{
    #region Provider Icons

    private static readonly IReadOnlyDictionary<string, AuthProviderConfig> DefaultAuthProviderIcons =
        new Dictionary<string, AuthProviderConfig>
        {
            ["Discord"] = new("Discord", "fa-brands fa-discord fa-align-left social-icon", style: "background: #5865F2; color: #fff;"),
            ["GitHub"] = new("GitHub", "fa-brands fa-github fa-align-left social-icon", style: "background: #000000; color: #fff;"),
            ["Google"] = new("Google", "fa-brands fa-google fa-align-left social-icon", style: "background: #d24228; color: #fff;"),
        };

    public static readonly IReadOnlyDictionary<string, AuthProviderConfig> AuthProviderIcons = LoadAuthProviderIcons();

    #endregion

    #region Properties

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public string? Class { get; set; }

    public string? Style { get; set; }

    #endregion

    #region Constructors

    public AuthProviderConfig()
    {
    }

    public AuthProviderConfig(string name, string icon, string? className = null, string? style = null)
    {
        Name = name;
        Icon = icon;
        Class = className ?? string.Empty;
        Style = style ?? string.Empty;
    }

    #endregion

    public static IReadOnlyDictionary<string, AuthProviderConfig> LoadAuthProviderIcons()
    {
        var path = Path.Combine(Strings.DataFolder, Strings.AuthProviderFileName);
        if (!File.Exists(path))
        {
            return DefaultAuthProviderIcons;
        }
        var data = File.ReadAllText(path);
        if (string.IsNullOrEmpty(data))
        {
            return DefaultAuthProviderIcons;
        }
        // Merge default with configured auth providers
        var obj = data.FromJson<Dictionary<string, AuthProviderConfig>>();
        if (obj == null)
        {
            return DefaultAuthProviderIcons;
        }
        var merged = DefaultAuthProviderIcons.Merge(obj, updateValues: true);
        return merged ?? DefaultAuthProviderIcons;
    }
}