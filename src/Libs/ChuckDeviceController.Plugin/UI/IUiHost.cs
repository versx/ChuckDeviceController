﻿namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Plugin host handler for executing user interface operations.
    /// </summary>
    public interface IUiHost
    {
        #region Properties

        /// <summary>
        /// Gets a list of navbar headers registered by plugins.
        /// </summary>
        IReadOnlyList<NavbarHeader> NavbarHeaders { get; }

        /// <summary>
        /// Gets a list of dashboard statistics registered by plugins.
        /// </summary>
        IReadOnlyList<IDashboardStatsItem> DashboardStatsItems { get; }

        /// <summary>
        /// Gets a list of dashboard tiles registered by plugins.
        /// </summary>
        IReadOnlyList<IDashboardTile> DashboardTiles { get; }

        /// <summary>
        /// Gets a list of settings tabs registered by plugins.
        /// </summary>
        IReadOnlyList<SettingsTab> SettingsTabs { get; }

        /// <summary>
        /// Gets a dictionary of settings properties for tabs registered by plugins.
        /// </summary>
        IReadOnlyDictionary<string, List<SettingsProperty>> SettingsProperties { get; }

        #endregion

        #region Methods

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

        /// <summary>
        /// Adds a statistic tile to the front page dashboard.
        /// </summary>
        /// <param name="tile">Dashboard statistics tile to add.</param>
        Task AddDashboardTileAsync(IDashboardTile tile);

        /// <summary>
        /// Adds a list of statistic tiles to the front page dashboard.
        /// </summary>
        /// <param name="tiles">List of dashboard statistic tiles to add.</param>
        Task AddDashboardTilesAsync(IEnumerable<IDashboardTile> tiles);

        // TODO: UpdateDashboardTileAsync?

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tab"></param>
        /// <returns></returns>
        Task AddSettingsTabAsync(SettingsTab tab);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tabId"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        Task AddSettingsPropertyAsync(string tabId, SettingsProperty property);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tabId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        Task AddSettingsPropertiesAsync(string tabId, IEnumerable<SettingsProperty> properties);

        #endregion
    }
}