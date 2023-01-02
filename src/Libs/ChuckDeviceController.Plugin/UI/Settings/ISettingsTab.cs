namespace ChuckDeviceController.Plugin;

/// <summary>
/// Settings tab interface contract for adding UI settings
/// from plugins to separate tabs.
/// </summary>
public interface ISettingsTab
{
    /// <summary>
    /// Gets or sets the unique ID of the tab.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets or sets the display text of the tab.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets or sets the html anchor tag name of the tab.
    /// </summary>
    /// <remarks>No hash symbol is needed.</remarks>
    string? Anchor { get; }

    /// <summary>
    /// Gets or sets the display index of the tab in the tab list.
    /// </summary>
    uint DisplayIndex { get; }

    /// <summary>
    /// Gets or sets the CSS class name to use.
    /// </summary>
    string? Class { get; }

    /// <summary>
    /// Gets or sets the raw CSS styling to use.
    /// </summary>
    string? Style { get; }
}