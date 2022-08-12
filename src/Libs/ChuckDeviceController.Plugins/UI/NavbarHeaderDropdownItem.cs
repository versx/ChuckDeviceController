namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// Navigation bar header dropdown item plugin contract implemention.
    /// </summary>
    public class NavbarHeaderDropdownItem : INavbarHeader
    {
        /// <summary>
        /// Gets or sets the text to display for this navbar
        /// header.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the controller name the action name
        /// should relate to.
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// Gets or sets the controller action name to execute
        /// when the navbar header is clicked.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the FontAwesome v6 icon key to use for 
        /// the navbar header dropdown item. https://fontawesome.com/icons
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the numeric display index order of
        /// the navbar header in the list of navbar headers.
        /// </summary>
        public uint DisplayIndex { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether to insert a dropdown
        /// separator instead of a dropdown item.
        /// </summary>
        public bool IsSeparator { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether the navbar
        /// header dropdown item is disabled or not.
        /// </summary>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Instantiates a new instance of a navbar header with
        /// dropdown items.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="controllerName"></param>
        /// <param name="actionName"></param>
        /// <param name="icon"></param>
        /// <param name="displayIndex"></param>
        /// <param name="isSeparator"></param>
        /// <param name="isDisabled"></param>
        public NavbarHeaderDropdownItem(string text, string controllerName = "", string actionName = "Index", string icon = "", uint displayIndex = 999, bool isSeparator = false, bool isDisabled = false)
        {
            Text = text;
            ControllerName = controllerName;
            ActionName = actionName;
            Icon = icon;
            DisplayIndex = displayIndex;
            IsSeparator = isSeparator;
            IsDisabled = isDisabled;
        }
    }
}