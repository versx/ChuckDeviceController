namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Navigation bar header plugin contract implementation.
    /// </summary>
    public class NavbarHeader : INavbarHeader
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
        /// the navbar header. https://fontawesome.com/icons
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the numeric display index order of
        /// the navbar header in the list of navbar headers.
        /// </summary>
        public uint DisplayIndex { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether the navbar
        /// header should be treated as a dropdown.
        /// </summary>
        public bool IsDropdown { get; set; }

        /// <summary>
        /// Gets or sets a list of navbar header dropdown items.
        /// </summary>
        public IEnumerable<NavbarHeader>? DropdownItems { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether the
        /// navbar header is disabled or not.
        /// </summary>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether to insert a
        /// separator instead of a dropdown item.
        /// </summary>
        public bool IsSeparator { get; set; }

        /// <summary>
        /// Instantiates a new navbar header instance using default 
        /// property values.
        /// </summary>
        public NavbarHeader()
        {
            Text = string.Empty;
            ControllerName = string.Empty;
            ActionName = string.Empty;
            Icon = string.Empty;
        }

        /// <summary>
        /// Instantiates a new navbar header instance using the specified
        /// property values.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="controllerName"></param>
        /// <param name="actionName"></param>
        /// <param name="icon"></param>
        /// <param name="displayIndex"></param>
        /// <param name="isDropdown"></param>
        /// <param name="dropdownItems"></param>
        /// <param name="isDisabled"></param>
        /// <param name="isSeparator"></param>
        public NavbarHeader(string text, string controllerName = "", string actionName = "Index", string icon = "", uint displayIndex = 999, bool isDropdown = false, IEnumerable<NavbarHeader>? dropdownItems = null, bool isDisabled = false, bool isSeparator = false)
        {
            Text = text;
            ControllerName = controllerName;
            ActionName = actionName;
            Icon = icon;
            DisplayIndex = displayIndex;
            IsDropdown = isDropdown;
            DropdownItems = dropdownItems;
            IsDisabled = isDisabled;
            IsSeparator = isSeparator;
        }
    }
}