namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// 
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

        // TODO: Add IsSeparateSection property
        // TODO: ControllerName
        // TODO: ActionName
    }
}