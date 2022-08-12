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
        /// Gets or sets the controller name the action name
        /// should relate to when the tile is clicked.
        /// </summary>
        public string ControllerName { get; }

        /// <summary>
        /// Gets or sets the controller action name to execute
        /// when the navbar header is clicked.
        /// </summary>
        public string ActionName { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <param name="icon"></param>
        /// <param name="controllerName"></param>
        /// <param name="actionName"></param>
        public DashboardTile(string text, string value, string icon = "", string controllerName = "", string actionName = "")
        {
            Text = text;
            Value = value;
            Icon = icon;
            ControllerName = controllerName;
            ActionName = actionName;
        }
    }
}