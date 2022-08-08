namespace ChuckDeviceController.Plugins
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUiHost
    {
        /// <summary>
        /// Gets a list of navbar headers registered by plugins
        /// </summary>
        IReadOnlyList<NavbarHeader> NavbarHeaders { get; }

        /// <summary>
        /// Gets a list of dashboard statistics registered by plugins
        /// </summary>
        IReadOnlyList<IDashboardStatsItem> DashboardStatsItems { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        Task AddNavbarHeaderAsync(NavbarHeader header);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers"></param>
        Task AddNavbarHeadersAsync(IEnumerable<NavbarHeader> headers);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stat"></param>
        Task AddDashboardStatisticAsync(IDashboardStatsItem stat);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stats"></param>
        Task AddDashboardStatisticsAsync(IEnumerable<IDashboardStatsItem> stats);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stat"></param>
        Task UpdateDashboardStatisticAsync(IDashboardStatsItem stat);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stats"></param>
        Task UpdateDashboardStatisticsAsync(IEnumerable<IDashboardStatsItem> stats);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface INavbarHeader
    {
        /// <summary>
        /// Gets the text displayed in the navbar
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the controller name related to the navbar header
        /// </summary>
        string ControllerName { get; }

        /// <summary>
        /// Gets the action to execute in the controller
        /// </summary>
        string ActionName { get; }

        /// <summary>
        /// Gets the display index in the navbar
        /// </summary>
        uint DisplayIndex { get; }
    }

    public class NavbarHeader : INavbarHeader
    {
        public string Text { get; set; }

        public string ControllerName { get; set; }

        public string ActionName { get; set; }

        public uint DisplayIndex { get; set; }

        public bool IsDropdown { get; set; }

        public IEnumerable<NavbarHeaderDropdownItem>? DropdownItems { get; set; }

        public NavbarHeader()
        {
        }

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

    public class NavbarHeaderDropdownItem : INavbarHeader
    {
        public string Text { get; set; }

        public string ControllerName { get; set; }

        public string ActionName { get; set; }

        public uint DisplayIndex { get; set; }

        public NavbarHeaderDropdownItem(string text, string controllerName = "", string actionName = "Index", uint displayIndex = 999)
        {
            Text = text;
            ControllerName = controllerName;
            ActionName = actionName;
            DisplayIndex = displayIndex;
        }
    }

    public interface IDashboardStatsItem
    {
        string Name { get; }

        string Value { get; }

        bool IsHtml { get; }
    }

    public class DashboardStatsItem : IDashboardStatsItem
    {
        public string Name { get; }

        public string Value { get; }

        public bool IsHtml { get; }

        public DashboardStatsItem(string name, string value, bool isHtml = false)
        {
            Name = name;
            Value = value;
            IsHtml = isHtml;
        }
    }
}