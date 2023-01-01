namespace ChuckDeviceController.Plugin;

using System.Text.Json.Serialization;

/// <summary>
/// Dashboard tile interface contract to display
/// custom tile-like elements on the dashboard.
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
    /// Gets or sets the Mvc controller name the action name
    /// should relate to when the tile is clicked.
    /// </summary>
    string ControllerName { get; }

    /// <summary>
    /// Gets or sets the Mvc controller action name to execute
    /// when the navbar header is clicked.
    /// </summary>
    string ActionName { get; }

    /// <summary>
    ///     Gets the function to update the value for the
    ///     dashboard tile.
    /// </summary>
    [JsonIgnore]
    Func<string> ValueUpdater { get; }

    // REVIEW: Add IsSeparateSection property
}