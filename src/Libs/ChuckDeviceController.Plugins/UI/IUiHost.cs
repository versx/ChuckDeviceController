namespace ChuckDeviceController.Plugins
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
}