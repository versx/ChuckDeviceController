namespace ChuckDeviceConfigurator.Utilities;

using ChuckDeviceConfigurator.Services.Icons;
using ChuckDeviceController.Plugin;

public static class UiUtils
{
    public static string GetPokemonIcon(
        uint pokemonId, uint formId = 0, ushort gender = 0, uint costumeId = 0,
        string width = "32", string height = "32", bool html = false)
    {
        var url = UIconsService.Instance.GetPokemonIcon(pokemonId, formId, 0, gender, costumeId);
        return html
            ? $"<img src='{url}' width='{width}' height='{height}' />"
            : url;
    }

    public static async Task<Dictionary<SettingsPropertyGroup, List<ISettingsProperty>>> GroupPropertiesAsync(IEnumerable<ISettingsProperty> properties)
    {
        var dict = new Dictionary<SettingsPropertyGroup, List<ISettingsProperty>>();
        foreach (var property in properties)
        {
            var group = property.Group ?? new();
            if (!dict.ContainsKey(group))
            {
                dict.Add(group, new() { property });
            }
            else
            {
                dict[group].Add(property);
                dict[group].Sort((a, b) => a.DisplayIndex.CompareTo(b.DisplayIndex));
            }
        }
        return await Task.FromResult(dict);
    }

    public static bool HasProperty(dynamic obj, string name)
    {
        var objType = obj.GetType();

        if (objType == typeof(System.Dynamic.ExpandoObject))
        {
            return ((IDictionary<string, object>)obj).ContainsKey(name);
        }

        var result = objType.GetProperty(name) != null;
        return result;
    }
}