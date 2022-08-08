namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Plugins;

    public class UiHost : IUiHost
    {
        private readonly ILogger<IUiHost> _logger;
        private static readonly List<NavbarHeader> _navbarHeaders = new();
        private static readonly Dictionary<string, IDashboardStatsItem> _dashboardStats = new();

        public IReadOnlyList<NavbarHeader> NavbarHeaders => _navbarHeaders;

        public IReadOnlyList<IDashboardStatsItem> DashboardStatsItems => _dashboardStats?.Values.ToList();

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
    }
}