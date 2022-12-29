namespace ChuckDeviceController.Plugin;

/// <summary>
/// <see cref="ISettingsTab"/> class implementation for adding
/// UI settings from plugins to separate tabs.
/// </summary>
public class SettingsTab : ISettingsTab
{
    /// <summary>
    /// Gets or sets the unique identifier of the tab.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the display text of the tab.
    /// </summary>
    public string Text { get; set; } = null!;

    /// <summary>
    /// Gets or sets the html anchor tag name of the tab.
    /// Note: No hash symbol needed.
    /// </summary>
    public string? Anchor { get; set; }

    /// <summary>
    /// Gets or sets the display index of the tab in the tab list.
    /// </summary>
    public uint DisplayIndex { get; set; }

    /// <summary>
    /// Gets or sets the CSS class name to use.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the raw CSS styling to use.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Instantiates a new instance of the <see cref="SettingsTab"/> class.
    /// </summary>
    public SettingsTab()
    {
    }

    /// <summary>
    /// Instantiates a new instance of the <see cref="SettingsTab"/> class.
    /// </summary>
    /// <param name="id">Unique identifier of the tab.</param>
    /// <param name="text">Text displayed for the tab header.</param>
    /// <param name="anchor">The HTML anchor tag name of the tab.</param>
    /// <param name="displayIndex">Display index of the tab in the tab list.</param>
    /// <param name="className">CSS class name to use.</param>
    /// <param name="style">Raw CSS styling to use.</param>
    public SettingsTab(string id, string text, string? anchor = null, uint displayIndex = 999, string? className = null, string? style = null)
    {
        Id = id;
        Text = text;
        Anchor = anchor;
        DisplayIndex = displayIndex;
        Class = className;
        Style = style;
    }
}