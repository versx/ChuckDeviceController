namespace ChuckDeviceController.Plugins
{
    public interface IUiHost
    {
        Task AddPathAsync();

        Task AddNavbarHeaderAsync(NavbarHeader header);
    }

    public class NavbarHeader : INavbarHeader
    {
        public string Text { get; set; }

        public string ControllerPath { get; set; }

        public uint DisplayIndex { get; set; }

        public bool IsDropdown { get; set; }

        public IEnumerable<NavbarHeaderDropdown> DropdownItems { get; set; }

        public NavbarHeader(string text, string controllerPath, uint displayIndex, bool isDropdown, IEnumerable<NavbarHeaderDropdown> dropdownItems)
        {
            Text = text;
            ControllerPath = controllerPath;
            DisplayIndex = displayIndex;
            IsDropdown = isDropdown;
            DropdownItems = dropdownItems;
        }
    }

    public class NavbarHeaderDropdown : INavbarHeader
    {
        public string Text { get; set; }

        public string ControllerPath { get; set; }

        public uint DisplayIndex { get; set; }

        public NavbarHeaderDropdown(string text, string controllerPath, uint displayIndex)
        {
            Text = text;
            ControllerPath = controllerPath;
            DisplayIndex = displayIndex;
        }
    }

    public interface INavbarHeader
    {
        string Text { get; }

        string ControllerPath { get; }

        uint DisplayIndex { get; }
    }
}