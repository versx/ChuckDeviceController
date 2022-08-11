﻿namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// Navigation bar header plugin contract.
    /// </summary>
    public interface INavbarHeader
    {
        /// <summary>
        /// Gets or sets the text to display for this navbar
        /// header.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets or sets the controller name the action name
        /// should relate to.
        /// </summary>
        string ControllerName { get; }

        /// <summary>
        /// Gets or sets the controller action name to execute
        /// when the navbar header is selected.
        /// </summary>
        string ActionName { get; }

        /// <summary>
        /// Gets or sets the FontAwesome v6 icon key to use for 
        /// the navbar header. https://fontawesome.com/icons
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// Gets or sets the numeric display index order of
        /// the navbar header in the list of navbar headers.
        /// </summary>
        uint DisplayIndex { get; }

        /// <summary>
        /// Gets or sets a value determining whether the
        /// navbar header is disabled or not.
        /// </summary>
        bool IsDisabled { get; }
    }
}