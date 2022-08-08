namespace ChuckDeviceController.Plugins
{
    public interface IUiHost
    {
        IReadOnlyList<NavbarHeader> NavbarHeaders { get; }

        Task AddPathAsync();

        Task AddNavbarHeaderAsync(NavbarHeader header);

        Task AddNavbarHeadersAsync(IEnumerable<NavbarHeader> headers);
    }

    public class NavbarHeader : INavbarHeader
    {
        public string Text { get; set; }

        public string ControllerName { get; set; }

        public string ActionName { get; set; }

        public uint DisplayIndex { get; set; }

        public bool IsDropdown { get; set; }

        public IEnumerable<NavbarHeaderDropdownItem> DropdownItems { get; set; }

        public NavbarHeader()
        {
        }

        public NavbarHeader(string text, string controllerName, string actionName, uint displayIndex, bool isDropdown, IEnumerable<NavbarHeaderDropdownItem> dropdownItems)
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

        public NavbarHeaderDropdownItem()
        {
        }

        public NavbarHeaderDropdownItem(string text, string controllerName, string actionName, uint displayIndex)
        {
            Text = text;
            ControllerName = controllerName;
            ActionName = actionName;
            DisplayIndex = displayIndex;
        }
    }

    public interface INavbarHeader
    {
        string Text { get; }

        string ControllerName { get; }

        string ActionName { get; }

        uint DisplayIndex { get; }
    }
}