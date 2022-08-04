namespace ChuckDeviceController.Plugins
{
    public interface IUiHost
    {
        Task AddPathAsync();

        Task AddNavbarHeaderAsync(NavbarHeaderOptions options);
    }

    public class NavbarHeaderOptions : INavbarHeader
    {
        public string Text { get; set; }

        public string ControllerPath { get; set; }

        public uint DisplayIndex { get; set; }

        public bool IsDropdown { get; set; }

        public IEnumerable<NavbarHeaderDropdownItem> DropdownItems { get; set; }
    }

    public class NavbarHeaderDropdownItem : INavbarHeader
    {
        public string Text { get; set; }

        public string ControllerPath { get; set; }

        public uint DisplayIndex { get; set; }
    }

    public interface INavbarHeader
    {
        string Text { get; }

        string ControllerPath { get; }

        uint DisplayIndex { get; }
    }
}