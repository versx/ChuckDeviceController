namespace ChuckDeviceController.Plugin;

/// <summary>
/// <see cref="IDashboardTile"/> class implementation to
/// display custom tile-like elements on the dashboard.
/// </summary>
public class DashboardTile : IDashboardTile
{
    /// <summary>
    /// Gets the text displayed for the dashboard tile.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the value for the dashboard tile.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the Fontawesome icon to display.
    /// </summary>
    public string Icon { get; }

    /// <summary>
    /// Gets the Mvc controller name the action name
    /// should relate to when the tile is clicked.
    /// </summary>
    public string ControllerName { get; }

    /// <summary>
    /// Gets the Mvc controller action name to execute
    /// when the navbar header is clicked.
    /// </summary>
    public string ActionName { get; }

    /// <summary>
    /// Instantiates a new instance of the <seealso cref="DashboardTile"/> class.
    /// </summary>
    /// <param name="text">The text displayed for the dashboard tile.</param>
    /// <param name="value">The value for the dashboard tile.</param>
    /// <param name="icon">Fontawesome icon to display.</param>
    /// <param name="controllerName">Mvc Controller name the action name
    /// should relate to when the tile is clicked.</param>
    /// <param name="actionName">Mvc controller action name to execute
    /// when the navbar header is clicked.</param>
    public DashboardTile(string text, string value, string icon = "", string controllerName = "", string actionName = "Index")
    {
        Text = text;
        Value = value;
        Icon = icon;
        ControllerName = controllerName;
        ActionName = actionName;
    }
}