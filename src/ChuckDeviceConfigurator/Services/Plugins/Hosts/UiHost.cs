﻿namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Plugins;

    public class UiHost : IUiHost
    {
        private readonly ILogger<IUiHost> _logger;
        private static readonly List<NavbarHeader> _navbarHeaders = new();
        private static readonly Dictionary<string, IDashboardStatsItem> _dashboardStats = new();
        private static readonly Dictionary<string, IDashboardTile> _dashboardTiles = new();

        public IReadOnlyList<NavbarHeader> NavbarHeaders => _navbarHeaders;

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
            if (_navbarHeaders.Contains(header))
            {
                _logger.LogWarning($"Navbar header '{header.Text}' already registered");
                return;
            }

            _navbarHeaders.Add(header);
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