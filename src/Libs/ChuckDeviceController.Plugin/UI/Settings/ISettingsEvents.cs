namespace ChuckDeviceController.Plugin;

/// <summary>
/// 
/// </summary>
public interface ISettingsPropertyEvents
{
    /// <summary>
    /// 
    /// </summary>
    void OnSave(IReadOnlyDictionary<string, List<ISettingsProperty>> properties);
}