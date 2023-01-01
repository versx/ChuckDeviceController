# Plugin Host Interface Contracts  

## IAuthorizeHost  

### Description  
- Create new user identity roles to restrict routing endpoints in plugins    

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

/// <summary>
/// User identity authorization host handler.
/// </summary>
public interface IAuthorizeHost
{
    /// <summary>
    ///     Registers a custom user role with the host application.
    /// </summary>
    /// <param name="name">
    ///     The name of the role to register.
    /// </param>
    /// <param name="displayIndex">
    ///     Display index value when listing roles.
    /// </param>
    /// <returns>
    ///     Returns a value determining whether the role was registered
    ///     or not.
    /// </returns>
    Task<bool> RegisterRole(string name, int displayIndex);
}
```

<hr>

## IConfigurationHost  

### Description  
- Load configuration files from file system
- Get property values from loaded configuration files

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

using Microsoft.Extensions.Configuration;

/// <summary>
///     This interface contract can be used by all plugin modules to load setting and configuration data.
/// 
///     The default implementation which is loaded if no other plugin registers an instance uses 
///     appsettings.json to store configuration data to be used by Plugins.
/// 
///     An instance of this interface is available via the DI container, any custom implementations
///     must be configured to be used in the DI contaner when being initialized.
/// </summary>
/// <remarks>
///     This class can be customized by the host application, if no implementation is provided then
///     a default implementation is provided.
/// </remarks>
public interface IConfigurationHost
{
    /// <summary>
    ///     Retrieves a configuration instance.
    /// </summary>
    /// <param name="jsonFileName">
    ///     Name of the JSON file name to be used. If a JSON cofiguration file is not provided, the default
    ///     'appsettings.json' will be loaded from the calling plugin's root folder.
    /// </param>
    /// <param name="sectionName">
    ///     The name of the configuration section that might be required.
    /// </param>
    /// <returns>
    ///     Configuration file instance initialized with the required settings.
    /// </returns>
    IConfiguration GetConfiguration(string? jsonFileName = null, string? sectionName = null);

    /// <summary>
    ///     Retrieves a value from a JSON configuration file.
    /// </summary>
    /// <typeparam name="T">The class related to the settings being requested.</typeparam>
    /// <param name="name">Name of the property to retrieve the value for.</param>
    /// <param name="defaultValue">Default value to return.</param>
    /// <param name="sectionName">The name of the configuration section that might be required.</param>
    /// <returns>Returns the value related to the named configuration property.</returns>
    T? GetValue<T>(string name, T? defaultValue = default, string? sectionName = null);
}
```

<hr>

## IDatabaseHost  

### Description  
- Retrieve all of a specified data entity (read only access)
- Retrieve a specific data entity by primary key (read only access)
- Retrieve a specific data entity by predicate/expression (read only access)

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

using System.Linq.Expressions;

using ChuckDeviceController.Data.Common;

/// <summary>
/// Plugin host handler contract used to interact with the database entities.
/// </summary>
public interface IDatabaseHost
{
    //IRepository<IAccount, string> Accounts { get; }
    //IRepository<IPokestop, string> Pokestops { get; }
    //IRepository<IDevice, string> Devices { get; }

    /// <summary>
    /// Gets a list of database entities.
    /// </summary>
    /// <typeparam name="TEntity">Database entity contract type.</typeparam>
    /// <returns>Returns a list of database entities.</returns>
    Task<IReadOnlyList<TEntity>> FindAllAsync<TEntity>();

    /// <summary>
    /// Gets a database entity by primary key.
    /// </summary>
    /// <typeparam name="TEntity">Database entity contract type.</typeparam>
    /// <typeparam name="TKey">Database entity primary key type.</typeparam>
    /// <param name="id">Primary key of the database entity.</param>
    /// <returns>Returns a database entity.</returns>
    Task<TEntity?> FindAsync<TEntity, TKey>(TKey id);

    /// <summary>
    /// Gets a list of database entities matching the specified criteria.
    /// </summary>
    /// <typeparam name="TKey">Entity property type when sorting.</typeparam>
    /// <typeparam name="TEntity">Database entity contract type.</typeparam>
    /// <param name="predicate">Predicate used to determine if a database entity matches.</param>
    /// <param name="order">Sort order expression. (Optional)</param>
    /// <param name="sortDirection">Sort ordering direction.</param>
    /// <param name="limit">Limit the returned number of results.</param>
    /// <returns>Returns a list of database entities.</returns>
    Task<IReadOnlyList<TEntity>> FindAsync<TEntity, TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>>? order = null,
        SortOrderDirection sortDirection = SortOrderDirection.Asc,
        int limit = 1000)
        where TEntity : class
        where TKey : notnull;
}
```

<hr>

## IEventAggregatorHost  

### Description  
- Publish events to the host application
- Publish events to other loaded plugins
- Subscribe to events via event service bus

### Class View  
```cs
namespace ChuckDeviceController.Plugin.EventBus;

/// <summary>
/// 
/// </summary>
public interface IEventAggregatorHost : IObservable<IEvent>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    void Publish(IEvent message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    IDisposable Subscribe(ICustomObserver<IEvent> observer);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observer"></param>
    /// <returns></returns>
    IDisposable Subscribe<T>(ICustomObserver<T> observer)
        where T : IEvent;
}
```

<hr>

## IFileStorageHost  

### Description  
- Load files from file system
- Save files to file system

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

/// <summary>
///     Interface contract used for reading data from as well as
///     persisting data to storage. The type of storage used will
///     depend on the implementation.
/// </summary>
public interface IFileStorageHost : ISaveData, ILoadData
{
}

/// <summary>
/// 
/// </summary>
public interface ILoadData
{
    /// <summary>
    ///     Loads file data of type T from the plugin's folder.
    /// </summary>
    /// <typeparam name="T">
    ///     Type of file data to be loaded.
    /// </typeparam>
    /// <param name="folderName">
    ///     Sub folder within plugin's folder, optional. If not set,
    ///     searches root of plugin's folder.
    /// </param>
    /// <param name="fileName">
    ///     File name of storage file to load, including extension
    ///     otherwise generic '.dat' extension will be appended.
    /// </param>
    /// <returns>
    ///     Type of data to be loaded or default type if exception occurs.
    /// </returns>
    T Load<T>(string folderName, string fileName);
}

/// <summary>
/// 
/// </summary>
public interface ISaveData
{
    /// <summary>
    ///     Saves file data of type T to the plugin's folder.
    /// </summary>
    /// <typeparam name="T">
    ///     Type of data to be saved.
    /// </typeparam>
    /// <param name="data">
    ///     File data to be saved.
    /// </param>
    /// <param name="folderName">
    ///     Sub folder within plugin's folder, optional. If not set,
    ///     uses root of plugin's folder.
    /// </param>
    /// <param name="name">
    ///     File name of storage file to save, including extension
    ///     otherwise generic '.dat' extension will be appended.
    /// </param>
    /// <param name="prettyPrint">
    /// </param>
    /// <returns>
    ///     Returns <c>true</c> if successful, otherwise <c>false</c>.
    /// </returns>
    bool Save<T>(T data, string folderName, string name, bool prettyPrint = false);
}
```

<hr>

## IGeofenceServiceHost  

### Description  
- Fetch geofences
- Create geofences
- Check if coordinate is in geofence(s)

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Geometry.Models.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IGeofenceServiceHost
{
    /// <summary>
    /// Create a new or update an existing geofence.
    /// </summary>
    /// <param name="options">Geofence options used to create or update.</param>
    Task CreateGeofenceAsync(IGeofence options);

    /// <summary>
    /// Retrieves a geofence from the database by name.
    /// </summary>
    /// <param name="name">Name of geofence to retrieve.</param>
    /// <returns>Returns a geofence interface contract.</returns>
    Task<IGeofence> GetGeofenceAsync(string name);

    /// <summary>
    /// Gets the geofence boundaries in multipolygon format as well as a two-dimensional list of coordinates.
    /// </summary>
    /// <param name="geofence">Geofence to get coordinates from.</param>
    /// <returns>Returns a tuple with a list of MultiPolygons and a two-dimensional list of coordinates.</returns>
    (IReadOnlyList<IMultiPolygon>, IReadOnlyList<IReadOnlyList<ICoordinate>>) GetMultiPolygons(IGeofence geofence);

    /// <summary>
    /// Gets the geofence location plots as a list of coordinates.
    /// </summary>
    /// <param name="geofence">Geofence to get coordinates from.</param>
    /// <returns>Returns a list of coordinates.</returns>
    IReadOnlyList<ICoordinate>? GetCoordinates(IGeofence geofence);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="multiPolygons"></param>
    /// <returns></returns>
    bool IsPointInMultiPolygons(ICoordinate coord, IEnumerable<IMultiPolygon> multiPolygons);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="multiPolygon"></param>
    /// <returns></returns>
    bool IsPointInMultiPolygon(ICoordinate coord, IMultiPolygon multiPolygon);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="coordinates"></param>
    /// <returns></returns>
    bool IsPointInPolygon(ICoordinate coord, IEnumerable<ICoordinate> coordinates);
}
```

<hr>

## IInstanceServiceHost  

### Description  
- Create new instances
- Update instances

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Data.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IInstanceServiceHost
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    Task CreateInstanceAsync(IInstance options);
}
```

<hr>

## IJobControllerServiceHost  

### Description  
- Register new custom job controller instance types
- Create new custom job controllers
- Assign devices to instances

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Common.Jobs;
using ChuckDeviceController.Data.Abstractions;

/// <summary>
/// Plugin host handler contract used to interact with and manage the
/// job controller service.
/// </summary>
public interface IJobControllerServiceHost : IInstanceServiceHost
{
    /// <summary>
    /// Gets a dictionary of active and configured devices.
    /// </summary>
    IReadOnlyDictionary<string, IDevice> Devices { get; }

    /// <summary>
    /// Gets a dictionary of all loaded job controller instances.
    /// </summary>
    IReadOnlyDictionary<string, IJobController> Instances { get; }

    /// <summary>
    /// Gets a list of all registered custom job controller instance types.
    /// </summary>
    IReadOnlyList<string> CustomInstanceTypes { get; }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="customInstanceType"></param>
    /// <returns></returns>
    Task RegisterJobControllerAsync<T>(string customInstanceType) where T : IJobController;

    /// <summary>
    /// Assigns the specified device to a specific job controller
    /// instance by name.
    /// </summary>
    /// <param name="device">Device entity.</param>
    /// <param name="instanceName">Job controller instance name.</param>
    Task AssignDeviceToJobControllerAsync(IDevice device, string instanceName);
}
```

<hr>

## ILocalizationHost  

### Description  
- Set currently loaded locale
- Retrieve translated localized text

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

using System.Globalization;

/// <summary>
/// Plugin host handler contract used to translate strings.
/// </summary>
public interface ILocalizationHost
{
    #region Properties

    /// <summary>
    /// Gets or sets the current culture localization to use.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the two letter ISO country code for the currently set localization.
    /// </summary>
    /// <value>The two letter ISO country code.</value>
    string CountryCode { get; }

    #endregion

    /// <summary>
    /// Sets the country locale code to use for translations.
    /// </summary>
    /// <param name="locale">Two letter ISO language name code.</param>
    void SetLocale(string locale);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    string Translate(string key);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyWithArgs"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    string Translate(string keyWithArgs, params object[] args);

    /// <summary>
    /// Translate a Pokemon id to name.
    /// </summary>
    /// <param name="pokemonId">Pokemon ID to translate to name.</param>
    /// <returns></returns>
    string GetPokemonName(uint pokemonId);

    /// <summary>
    /// Translate a Pokemon form id to name.
    /// </summary>
    /// <param name="formId">Form ID to translate to name.</param>
    /// <param name="includeNormal">Include 'Normal' form name or not.</param>
    /// <returns></returns>
    string GetFormName(uint formId, bool includeNormal = false);

    /// <summary>
    /// Translate a Pokemon costume id to name.
    /// </summary>
    /// <param name="costumeId">Costume ID to translate to name.</param>
    /// <returns></returns>
    string GetCostumeName(uint costumeId);

    /// <summary>
    /// Translate a Pokemon evolution id to name.
    /// </summary>
    /// <param name="evolutionId">Evolution ID to translate to name.</param>
    /// <returns></returns>
    string GetEvolutionName(uint evolutionId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="moveId"></param>
    /// <returns></returns>
    string GetMoveName(uint moveId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="throwTypeId"></param>
    /// <returns></returns>
    string GetThrowName(uint throwTypeId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="itemId"></param>
    /// <returns></returns>
    string GetItem(uint itemId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="weatherConditionId"></param>
    /// <returns></returns>
    string GetWeather(uint weatherConditionId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="alignmentTypeId"></param>
    /// <returns></returns>
    string GetAlignmentName(uint alignmentTypeId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="characterCategoryId"></param>
    /// <returns></returns>
    string GetCharacterCategoryName(uint characterCategoryId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="invasionCharacterId"></param>
    /// <returns></returns>
    string GetGruntType(uint invasionCharacterId);
}
```

<hr>

## ILoggingHost  

### Description  
- Log trace messages
- Log info messages
- Log debug messages
- Log warning messages
- Log error messages
- Log critical messages

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

/// <summary>
/// Plugin host handler for logging messages from plugins.
/// </summary>
public interface ILoggingHost
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogTrace(string message, params object?[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogInformation(string message, params object?[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogDebug(string message, params object?[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogWarning(string message, params object?[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogError(string message, params object?[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogError(Exception error, string? message = null, params object?[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogCritical(string message, params object?[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void LogCritical(Exception error, string? message = null, params object?[] args);
}
```

<hr>

## IMemoryCacheHost  
### Description  
- Cache data in-memory  

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

/// <summary>
/// In memory cache host handler.
/// </summary>
public interface IMemoryCacheHost
{
    /// <summary>
    /// Checks whether a key exists in the cache.
    /// </summary>
    /// <param name="key">Key to check if exists.</param>
    /// <returns>Returns <c>true</c> if the key exists, otherwise <c>false</c>.</returns>
    bool IsSet(string key);

    /// <summary>
    /// Trys to retrieve a value by key from the cache.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="key">Key to check.</param>
    /// <param name="value">Value returned from the cache.</param>
    /// <returns>Returns <c>true</c> if the key exists, otherwise <c>false</c>.</returns>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>
    /// Retrieve a value by key from the cache.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="key">Key to check.</param>
    /// <returns>Returns a value from the cache, otherwise <c>null</c>.</returns>
    T? GetValue<T>(string key);

    /// <summary>
    /// Caches a value by key with a set expiration time.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="key">Key to set.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="expiryS">Expiration time in seconds.</param>
    void SetValue<T>(string key, T value, ushort expiryS);

    /// <summary>
    /// Remove a entry from the cache by key.
    /// </summary>
    /// <param name="key">Key to remove from the cache.</param>
    void Remove(string key);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    void Clear();
}
```

<hr>

## IRoutingHost  

### Description  
- Generate routes (random, dynamic, bootstrap style, and POI focused)
- Optimize routes

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Geometry.Models.Abstractions;

/// <summary>
/// Route generator plugin host.
/// </summary>
public interface IRoutingHost
{
    /// <summary>
    ///     Generates a route using the specified route generator options.
    /// </summary>
    /// <param name="options">Routing generation options to use.</param>
    /// <returns>Returns a list of coordinates of the generated route.</returns>
    List<ICoordinate> GenerateRoute(RouteGeneratorOptions options);

    // TODO: OptimizeRoute

    // TODO: Clusters
}
```

<hr>

## IUIconsHost  

### Description  
- Retrieve/generate Pokemon icon urls  

### Class View  
```cs
namespace ChuckDeviceController.Plugin;

/// <summary>
/// UIcons standard host handler to retrieve icon url endpoints for plugins.
/// </summary>
public interface IUIconsHost
{
    /// <summary>
    /// Gets an icon image url based on the provided Pokemon details.
    /// </summary>
    /// <param name="pokemonId">Pokemon pokedex id.</param>
    /// <param name="formId">Pokemon form id.</param>
    /// <param name="evolutionId">Pokemon mega evolution id.</param>
    /// <param name="gender">Pokemon gender id.</param>
    /// <param name="costumeId">Pokemon costume id.</param>
    /// <param name="shiny">Whether the Pokemon is shiny or not.</param>
    /// <returns>Returns a url of the Pokemon image.</returns>
    string GetPokemonIcon(uint pokemonId, uint formId = 0, uint evolutionId = 0, uint gender = 0, uint costumeId = 0, bool shiny = false);
}
```

<hr>

## IUiHost  

### Description  
- Add Dashboard statistics
- Add Dashboard tiles
- Add Sidebar menu items and pages (including endless dropdowns)
- Add new global settings as well as tabs to settings page

### Class View  
```cs
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
```
