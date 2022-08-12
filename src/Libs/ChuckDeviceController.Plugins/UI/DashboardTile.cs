namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// 
    /// </summary>
    public class DashboardTile : IDashboardTile
    {
        /// <summary>
        /// Gets or sets the text displayed for the dashboard tile.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets or sets the value for the dashboard tile.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets or sets the Fontawesome icon to display.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <param name="icon"></param>
        public DashboardTile(string text, string value, string icon = "")
        {
            Text = text;
            Value = value;
            Icon = icon;
        }
    }
}