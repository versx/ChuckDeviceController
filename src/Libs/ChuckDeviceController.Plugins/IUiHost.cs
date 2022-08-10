﻿namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// Plugin host handler for executing user interface operations.
    /// </summary>
    public interface IUiHost
    {
        /// <summary>
        /// Gets a list of navbar headers registered by plugins.
        /// </summary>
        IReadOnlyList<NavbarHeader> NavbarHeaders { get; }

        /// <summary>
        /// Gets a list of dashboard statistics registered by plugins.
        /// </summary>
        IReadOnlyList<IDashboardStatsItem> DashboardStatsItems { get; }


        /// <summary>
        /// Adds a <seealso cref="NavbarHeader"/> item to the main
        /// application's Mvc navbar header.
        /// </summary>
        /// <param name="header">Navbar to add.</param>
        Task AddNavbarHeaderAsync(NavbarHeader header);

        /// <summary>
        /// Adds a list of <seealso cref="NavbarHeader"/> items to the
        /// main application's Mvc navbar header.
        /// </summary>
        /// <param name="headers">List of navbars to add.</param>
        Task AddNavbarHeadersAsync(IEnumerable<NavbarHeader> headers);

        /// <summary>
        /// Adds a custom <seealso cref="IDashboardStatsItem"/> to the
        /// dashboard front page.
        /// </summary>
        /// <param name="stat">Dashboard statistics item to add.</param>
        Task AddDashboardStatisticAsync(IDashboardStatsItem stat);

        /// <summary>
        /// Adds a list of <seealso cref="IDashboardStatsItem"/> items to
        /// the dashboard front page.
        /// </summary>
        /// <param name="stats">List of dashboard statistic items to add.</param>
        Task AddDashboardStatisticsAsync(IEnumerable<IDashboardStatsItem> stats);

        /// <summary>
        /// Update an existing <seealso cref="IDashboardStatsItem"/> item
        /// on the dashboard front page.
        /// </summary>
        /// <param name="stat">Dashboard statistics item to update.</param>
        Task UpdateDashboardStatisticAsync(IDashboardStatsItem stat);

        /// <summary>
        /// Update a list of existing <seealso cref="IDashboardStatsItem"/> items
        /// on the dashboard front page.
        /// </summary>
        /// <param name="stats">List of dashboard statistic items to update.</param>
        Task UpdateDashboardStatisticsAsync(IEnumerable<IDashboardStatsItem> stats);
    }

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
        /// Gets or sets the numeric display index order of
        /// the navbar header in the list of navbar headers.
        /// </summary>
        uint DisplayIndex { get; }
        // theme - dark/light
        // drop direction - dropdown (default), dropstart (left), dropup, dropend (right)
        // separator //<li><hr class="dropdown-divider"></li>
        // disabled
        // fontawesome icon
    }

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
        /// when the navbar header is selected.
        /// </summary>
        public string ActionName { get; set; }

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
        public IEnumerable<NavbarHeaderDropdownItem>? DropdownItems { get; set; }

        /// <summary>
        /// Instantiates a new navbar header instance using default 
        /// property values.
        /// </summary>
        public NavbarHeader()
        {
            Text = string.Empty;
            ControllerName = string.Empty;
            ActionName = string.Empty;
        }

        /// <summary>
        /// Instantiates a new navbar header instance using the specified
        /// property values.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="controllerName"></param>
        /// <param name="actionName"></param>
        /// <param name="displayIndex"></param>
        /// <param name="isDropdown"></param>
        /// <param name="dropdownItems"></param>
        public NavbarHeader(string text, string controllerName = "", string actionName = "Index", uint displayIndex = 999, bool isDropdown = false, IEnumerable<NavbarHeaderDropdownItem>? dropdownItems = null)
        {
            Text = text;
            ControllerName = controllerName;
            ActionName = actionName;
            DisplayIndex = displayIndex;
            IsDropdown = isDropdown;
            DropdownItems = dropdownItems;
        }
    }

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
        /// when the navbar header is selected.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the numeric display index order of
        /// the navbar header in the list of navbar headers.
        /// </summary>
        public uint DisplayIndex { get; set; }

        /// <summary>
        /// Instantiates a new instance of a navbar header with
        /// dropdown items.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="controllerName"></param>
        /// <param name="actionName"></param>
        /// <param name="displayIndex"></param>
        public NavbarHeaderDropdownItem(string text, string controllerName = "", string actionName = "Index", uint displayIndex = 999)
        {
            Text = text;
            ControllerName = controllerName;
            ActionName = actionName;
            DisplayIndex = displayIndex;
        }
    }

    /// <summary>
    /// Dashboard statistics item for displaying information
    /// on the front page.
    /// </summary>
    public interface IDashboardStatsItem
    {
        /// <summary>
        /// Gets or sets the name or title of the statistic.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the value of the statistic.
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Gets or sets a value determining whether the name
        /// and value properties include raw HTML or not.
        /// </summary>
        bool IsHtml { get; }
    }

    /// <summary>
    /// Dashboard statistics item for displaying information
    /// on the front page.
    /// </summary>
    public class DashboardStatsItem : IDashboardStatsItem
    {
        /// <summary>
        /// Gets or sets the name or title of the statistic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the value of the statistic.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets or sets a value determining whether the name
        /// and value properties include raw HTML or not.
        /// </summary>
        public bool IsHtml { get; }

        /// <summary>
        /// Instantiates a new dashboard statistics item using
        /// the provided property values.
        /// </summary>
        /// <param name="name">Name of the statistic.</param>
        /// <param name="value">Value of the statistic.</param>
        /// <param name="isHtml">Whether the name or value contain raw HTML.</param>
        public DashboardStatsItem(string name, string value, bool isHtml = false)
        {
            Name = name;
            Value = value;
            IsHtml = isHtml;
        }
    }
}