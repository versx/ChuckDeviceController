namespace ChuckDeviceController.Plugin;

/// <summary>
/// Plugin host handler for executing user interface operations.
/// </summary>
public interface IUiHost
{
    #region Properties

    /// <summary>
    /// Gets a list of sidebar items registered by plugins.
    /// </summary>
    IReadOnlyList<SidebarItem> SidebarItems { get; }

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
    IReadOnlyList<ISettingsTab> SettingsTabs { get; }

    /// <summary>
    /// Gets a dictionary of settings properties for tabs registered by plugins.
    /// </summary>
    IReadOnlyDictionary<string, List<ISettingsProperty>> SettingsProperties { get; }

    #endregion

    #region Sidebar

    /// <summary>
    /// Adds a <seealso cref="SidebarItem"/> item to the main
    /// application's Mvc sidebar.
    /// </summary>
    /// <param name="header">Sidebar item to add.</param>
    Task AddSidebarItemAsync(SidebarItem header);

    /// <summary>
    /// Adds a list of <seealso cref="SidebarItem"/> items to the
    /// main application's Mvc sidebar.
    /// </summary>
    /// <param name="headers">List of sidebar items to add.</param>
    Task AddSidebarItemsAsync(IEnumerable<SidebarItem> headers);

    #endregion

    #region Dashboard Statistics

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

    #endregion

    #region Dashboard Tiles

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

    #endregion

    #region Settings

    /// <summary>
    /// Adds a new settings tab.
    /// </summary>
    /// <param name="tab">Settings tab to add.</param>
    Task AddSettingsTabAsync(SettingsTab tab);

    /// <summary>
    /// Adds a settings property to an existing settings tab.
    /// </summary>
    /// <param name="tabId">Unique identifier of the destination tab.</param>
    /// <param name="property">Settings property to add to the destination tab.</param>
    Task AddSettingsPropertyAsync(string tabId, SettingsProperty property);

    /// <summary>
    /// Adds a list of settings properties to an existing settings tab.
    /// </summary>
    /// <param name="tabId">Unique identifier of the destination tab.</param>
    /// <param name="properties">List of settings properties to add to the destination.</param>
    Task AddSettingsPropertiesAsync(string tabId, IEnumerable<SettingsProperty> properties);

    /// <summary>
    /// Gets the value of a settings property by name.
    /// </summary>
    /// <typeparam name="T">Expected return type of the settings property.</typeparam>
    /// <param name="name">Name of the property to get the value of.</param>
    T? GetSettingsPropertyValue<T>(string name);

    #endregion
}