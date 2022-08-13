namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Plugins;

    public class PluginUiCache<T> : Dictionary<string, Dictionary<string, T>>
    {
    }

    public class UiHost : IUiHost
    {
        private readonly ILogger<IUiHost> _logger;
        // TODO: Index dictionaries by plugin name i.e. Dictionary<string, Dictionary<string, Interface>>
        //private static readonly PluginUiCache<NavbarHeader> _navbarHeaders = new();
        //private static readonly PluginUiCache<IDashboardStatsItem> _dashboardStats = new();
        //private static readonly PluginUiCache<IDashboardTile> _dashboardTiles = new();
        private static readonly Dictionary<string, NavbarHeader> _navbarHeaders = new();
        private static readonly Dictionary<string, IDashboardStatsItem> _dashboardStats = new();
        private static readonly Dictionary<string, IDashboardTile> _dashboardTiles = new();

        public IReadOnlyList<NavbarHeader> NavbarHeaders => _navbarHeaders?.Values.ToList();

        public IReadOnlyList<IDashboardStatsItem> DashboardStatsItems => _dashboardStats?.Values.ToList();

        public IReadOnlyList<IDashboardTile> DashboardTiles => _dashboardTiles?.Values.ToList();

        public UiHost(ILogger<IUiHost> logger)
        {
            _logger = logger;
        }

        public async Task AddNavbarHeadersAsync(IEnumerable<NavbarHeader> headers)
        {
            foreach (var header in headers)
            {
                await AddNavbarHeaderAsync(header);
            }
        }

        public async Task AddNavbarHeaderAsync(NavbarHeader header)
        {
            if (_navbarHeaders.ContainsKey(header.Text))
            {
                if (!header.IsDropdown)
                {
                    _logger.LogWarning($"Navbar header '{header.Text}' already registered");
                    return;
                }

                // Add dropdown items to existing headers
                var existingHeader = _navbarHeaders[header.Text];
                var newDropdownItems = new List<NavbarHeaderDropdownItem>();
                if (existingHeader.DropdownItems != null)
                {
                    newDropdownItems.AddRange(existingHeader.DropdownItems);
                }
                if (header.DropdownItems != null)
                {
                    newDropdownItems.AddRange(header.DropdownItems);
                }
                _navbarHeaders[header.Text].DropdownItems = newDropdownItems;
            }
            else
            {
                _navbarHeaders.Add(header.Text, header);
            }
            await Task.CompletedTask;
        }

        public async Task AddDashboardStatisticsAsync(IEnumerable<IDashboardStatsItem> stats)
        {
            foreach (var stat in stats)
            {
                await AddDashboardStatisticAsync(stat);
            }
        }

        public async Task AddDashboardStatisticAsync(IDashboardStatsItem stats)
        {
            if (_dashboardStats.ContainsKey(stats.Name))
            {
                // Already exists with name
                return;
            }

            _dashboardStats.Add(stats.Name, stats);
            await Task.CompletedTask;
        }

        public async Task UpdateDashboardStatisticsAsync(IEnumerable<IDashboardStatsItem> stats)
        {
            foreach (var stat in stats)
            {
                await UpdateDashboardStatisticAsync(stat);
            }
        }

        public async Task UpdateDashboardStatisticAsync(IDashboardStatsItem stats)
        {
            if (!_dashboardStats.ContainsKey(stats.Name))
            {
                // Does not exist
                return;
            }

            _dashboardStats[stats.Name] = stats;
            await Task.CompletedTask;
        }

        public async Task AddDashboardTileAsync(IDashboardTile tile)
        {
            if (_dashboardTiles.ContainsKey(tile.Text))
            {
                // Already exists with name
                return;
            }

            _dashboardTiles.Add(tile.Text, tile);
            await Task.CompletedTask;
        }

        public async Task AddDashboardTilesAsync(IEnumerable<IDashboardTile> tiles)
        {
            foreach (var tile in tiles)
            {
                await AddDashboardTileAsync(tile);
            }
        }
    }
}