namespace ChuckDeviceController.Plugin;

/// <summary>
/// Dashboard tile interface contract.
/// </summary>
public interface IDashboardTile
{
    /// <summary>
    /// Gets or sets the text displayed for the dashboard tile.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets or sets the value for the dashboard tile.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Gets or sets the Fontawesome icon to display.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Gets or sets the controller name the action name
    /// should relate to when the tile is clicked.
    /// </summary>
    string ControllerName { get; }

    /// <summary>
    /// Gets or sets the controller action name to execute
    /// when the navbar header is clicked.
    /// </summary>
    string ActionName { get; }

    // TODO: Add IsSeparateSection property
}