namespace ChuckDeviceController.Plugin;

/// <summary>
/// <see cref="ISettingsPropertyGroup"/> class implementation
/// for grouping <seealso cref="ISettingsProperty"/>.
/// </summary>
public class SettingsPropertyGroup : ISettingsPropertyGroup, IEquatable<SettingsPropertyGroup>
{
    /// <summary>
    /// Gets or sets the unique identifier for the settings property group.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the text to display for the settings property group.
    /// </summary>
    public string Text { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value used for sorting each HTML element
    /// created for the properties.
    /// </summary>
    public uint DisplayIndex { get; set; }

    /// <summary>
    /// Instantiates a new instance of the <see cref="SettingsPropertyGroup"/> class.
    /// </summary>
    public SettingsPropertyGroup()
    {
    }

    /// <summary>
    /// Instantiates a new instance of the <see cref="SettingsPropertyGroup"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for the settings property group.</param>
    /// <param name="text">Text displayed for the settings property group.</param>
    /// <param name="displayIndex">Sorting index used with each HTML element created for the grouped properties.</param>
    public SettingsPropertyGroup(string id, string text, uint displayIndex)
    {
        Id = id;
        Text = text;
        DisplayIndex = displayIndex;
    }

    #region IEquatable Implementation

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return (int)((Id?.GetHashCode() ?? 0) ^ (Text?.GetHashCode() ?? 0) ^ DisplayIndex);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        var other = obj as SettingsPropertyGroup;
        return Equals(other);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(SettingsPropertyGroup? other)
    {
        var result = other != null && Text == other.Text;
        return result;
    }

    #endregion
}