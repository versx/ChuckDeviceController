namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Side navigation bar plugin contract.
    /// </summary>
    public interface ISidebarItem
    {
        /// <summary>
        /// Gets or sets the text to display for this sidebar
        /// item.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets or sets the controller name the action name
        /// should relate to.
        /// </summary>
        string ControllerName { get; }

        /// <summary>
        /// Gets or sets the controller action name to execute
        /// when the sidebar item is clicked.
        /// </summary>
        string ActionName { get; }

        /// <summary>
        /// Gets or sets the FontAwesome v6 icon key to use for 
        /// the sidebar item. https://fontawesome.com/icons
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// Gets or sets the numeric display index order of
        /// the sidebar item in the list of sidebar items.
        /// </summary>
        uint DisplayIndex { get; }

        /// <summary>
        /// Gets or sets a value determining whether the
        /// sidebar item is disabled or not.
        /// </summary>
        bool IsDisabled { get; }

        /// <summary>
        /// Gets or sets a value determining whether to insert a
        /// separator instead of a dropdown item.
        /// </summary>
        bool IsSeparator { get; }

        //// <summary>
        //// Gets or sets a value determining whether to 
        //// create the navbar header in its own section
        //// or add it to the default.
        //// </summary>
        //bool IsSeparateSection { get; }
    }
}