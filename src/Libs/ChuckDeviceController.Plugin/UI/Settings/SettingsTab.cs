namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Settings tab interface contract to add UI settings for plugins.
    /// </summary>
    public class SettingsTab : ISettingsTab
    {
        /// <summary>
        /// Gets or sets the unique ID of the tab.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display text of the tab.
        /// </summary>
        public string Text { get; set; }

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
        /// 
        /// </summary>
        public SettingsTab()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="text"></param>
        /// <param name="anchor"></param>
        /// <param name="displayIndex"></param>
        /// <param name="className"></param>
        /// <param name="style"></param>
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
}