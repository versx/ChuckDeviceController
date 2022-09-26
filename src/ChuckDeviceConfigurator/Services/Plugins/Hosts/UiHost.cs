namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Plugin;

    public class PluginUiCache<T> : Dictionary<string, Dictionary<string, T>>
    {
    }

    public class UiHost : IUiHost
    {
        #region Variables

        private static readonly ILogger<IUiHost> _logger =
            new Logger<IUiHost>(LoggerFactory.Create(x => x.AddConsole()));
        // TODO: Index dictionaries by plugin name using PluginUiCache
        //private static readonly PluginUiCache<SidebarItem> _sidebarItems = new();
        //private static readonly PluginUiCache<IDashboardStatsItem> _dashboardStats = new();
        //private static readonly PluginUiCache<IDashboardTile> _dashboardTiles = new();
        private static readonly Dictionary<string, SidebarItem> _sidebarItems = new();
        private static readonly Dictionary<string, IDashboardStatsItem> _dashboardStats = new();
        private static readonly Dictionary<string, IDashboardTile> _dashboardTiles = new();
        private static readonly Dictionary<string, SettingsTab> _settingsTabs = new();
        private static readonly Dictionary<string, List<SettingsProperty>> _settingsProperties = new();

        #endregion

        #region Properties

        public IReadOnlyList<SidebarItem> SidebarItems => _sidebarItems?.Values.ToList();

        public IReadOnlyList<IDashboardStatsItem> DashboardStatsItems => _dashboardStats?.Values.ToList();

        public IReadOnlyList<IDashboardTile> DashboardTiles => _dashboardTiles?.Values.ToList();

        public IReadOnlyList<SettingsTab> SettingsTabs => _settingsTabs?.Values.ToList();

        public IReadOnlyDictionary<string, List<SettingsProperty>> SettingsProperties => _settingsProperties;

        #endregion

        #region Constructor

        public UiHost()
        {
            // Load host applications default sidebar nav headers
            Task.Run(async () => await LoadDefaultUiAsync());
        }

        #endregion

        #region Public Methods

        #region Sidebar

        public async Task AddSidebarItemsAsync(IEnumerable<SidebarItem> items)
        {
            foreach (var item in items)
            {
                await AddSidebarItemAsync(item);
            }
        }

        public async Task AddSidebarItemAsync(SidebarItem item)
        {
            if (!_sidebarItems.ContainsKey(item.Text))
            {
                _sidebarItems.Add(item.Text, item);
                return;
            }

            if (!item.IsDropdown)
            {
                _logger.LogWarning($"Sidebar item '{item.Text}' already registered");
                return;
            }

            // Add dropdown items to existing items
            var existingItem = _sidebarItems[item.Text];
            var newDropdownItems = new List<SidebarItem>();
            if (existingItem.DropdownItems != null)
            {
                newDropdownItems.AddRange(existingItem.DropdownItems);
            }
            if (item.DropdownItems != null)
            {
                newDropdownItems.AddRange(item.DropdownItems);
            }
            _sidebarItems[item.Text].DropdownItems = newDropdownItems;
            await Task.CompletedTask;
        }

        #endregion

        #region Dashboard Statistics

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
            // TODO: Update Dashboard stat in realtime without page refresh
            await Task.CompletedTask;
        }

        #endregion

        #region Dashboard Tiles

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

        #endregion

        #region Settings

        public async Task AddSettingsTabAsync(SettingsTab tab)
        {
            if (_settingsTabs.ContainsKey(tab.Id))
            {
                // Already exists with tab id
                return;
            }

            _settingsTabs.Add(tab.Id, tab);
            await Task.CompletedTask;
        }

        public async Task AddSettingsPropertyAsync(string tabId, SettingsProperty property)
        {
            if (!_settingsProperties.ContainsKey(tabId))
            {
                _settingsProperties.Add(tabId, new());
            }

            if (!_settingsProperties[tabId].Contains(property))
            {
                _settingsProperties[tabId].Add(property);
            }
            await Task.CompletedTask;
        }

        public async Task AddSettingsPropertiesAsync(string tabId, IEnumerable<SettingsProperty> properties)
        {
            foreach (var property in properties)
            {
                await AddSettingsPropertyAsync(tabId, property);
            }
        }

        public T? GetSettingsPropertyValue<T>(string name)
        {
            var properties = _settingsProperties.Values.SelectMany(x => x);
            var property = properties.FirstOrDefault(x => x.Name == name);
            var value = property?.Value ?? property?.DefaultValue;
            return (T?)value;
        }

        #endregion

        #endregion

        #region Private Methods

        private async Task LoadDefaultUiAsync()
        {
            var sidebarItems = new List<SidebarItem>
            {
                new("Home", "Home", displayIndex: 0, icon: "fa-solid fa-fw fa-house"),
                new("Accounts", "Account", displayIndex: 1, icon: "fa-solid fa-fw fa-user"),
                //new("API Keys", "ApiKey", displayIndex: 2, icon: "fa-solid fa-fw fa-key"),
                new("Devices", displayIndex: 2, icon: "fa-solid fa-fw fa-mobile-alt", isDropdown: true, dropdownItems: new List<SidebarItem>
                {
                    new("Devices", "Device", "Index", displayIndex: 0, icon: "fa-solid fa-fw fa-mobile-alt"),
                    new("Device Groups", "DeviceGroup", "Index", displayIndex: 1, icon: "fa-solid fa-fw fa-layer-group"),
                }),
                new("Instances", displayIndex: 3, icon: "fa-solid fa-fw fa-cubes-stacked", isDropdown: true, dropdownItems: new List<SidebarItem>
                {
                    new("Geofences", "Geofence", "Index", displayIndex: 0, icon: "fa-solid fa-fw fa-map-marked"),
                    new("Instances", "Instance", "Index", displayIndex: 1, icon: "fa-solid fa-fw fa-cubes-stacked"),
                    new("IV Lists", "IvList", "Index", displayIndex: 2, icon: "fa-solid fa-fw fa-list"),
                }),
                new("Plugins", "Plugin", displayIndex: 4, icon: "fa-solid fa-fw fa-puzzle-piece"),
                new("Schedules", displayIndex: 5, icon: "fa-solid fa-fw fa-calendar-days", isDropdown: true, dropdownItems: new List<SidebarItem>
                {
                    new("Assignments", "Assignment", "Index", displayIndex: 0, icon: "fa-solid fa-fw fa-cog"),
                    new("Assignment Groups", "AssignmentGroup", "Index", displayIndex: 1, icon: "fa-solid fa-fw fa-cogs"),
                }),
                new("Webhooks", "Webhook", displayIndex: 6, icon: "fa-solid fa-fw fa-circle-nodes"),
                new("Users", "User", displayIndex: 7, icon: "fa-solid fa-fw fa-users"),
                new("Utilities", displayIndex: 8, icon: "fa-solid fa-fw fa-toolbox", isDropdown: true, dropdownItems: new List<SidebarItem>
                {
                    new("Clear Quests", "Utilities", "ClearQuests", displayIndex: 0, icon: "fa-solid fa-fw fa-broom"),
                    new("Convert Forts", "Utilities", "ConvertForts", displayIndex: 1, icon: "fa-solid fa-fw fa-arrows-up-down"),
                    new("Clear Stale Pokestops", "Utilities", "ClearStalePokestops", displayIndex: 2, icon: "fa-solid fa-fw fa-clock"),
                    new("Reload Instance", "Utilities", "ReloadInstance", displayIndex: 3, icon: "fa-solid fa-fw fa-rotate"),
                    new("Truncate Data", "Utilities", "TruncateData", displayIndex: 4, icon: "fa-solid fa-fw fa-trash-can"),
                    new("Re-Quest", "Utilities", "ReQuest", displayIndex: 5, icon: "fa-solid fa-fw fa-clock-rotate-left", isDisabled: true),
                    new("Route Generator", "Utilities", "RouteGenerator", displayIndex: 6, icon: "fa-solid fa-fw fa-route"),
                }),
            };
            await AddSidebarItemsAsync(sidebarItems);

            var settingsTab = new SettingsTab(
                id: "general",
                text: "General",
                anchor: "general",
                displayIndex: 0,
                className: "active"
            );
            await AddSettingsTabAsync(settingsTab);

            var settingsGroup = new SettingsPropertyGroup("general", "General", 0);
            var settingsTabProperties = new List<SettingsProperty>
            {
                new("Enabled", "general-enabled", SettingsPropertyType.CheckBox, value: true, group: settingsGroup),
                new("Url", "general-url", SettingsPropertyType.Text, group: settingsGroup),
            };
            await AddSettingsPropertiesAsync(settingsTab.Id, settingsTabProperties);
        }

        #endregion
    }
}