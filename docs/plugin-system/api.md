<a name='assembly'></a>
# Plugin API Reference

## Contents

- [DashboardStatsItem](#T-ChuckDeviceController-Plugin-DashboardStatsItem 'ChuckDeviceController.Plugin.DashboardStatsItem')
    - [#ctor(name,value,isHtml)](#M-ChuckDeviceController-Plugin-DashboardStatsItem-#ctor-System-String,System-String,System-Boolean- 'ChuckDeviceController.Plugin.DashboardStatsItem.#ctor(System.String,System.String,System.Boolean)')
    - [IsHtml](#P-ChuckDeviceController-Plugin-DashboardStatsItem-IsHtml 'ChuckDeviceController.Plugin.DashboardStatsItem.IsHtml')
    - [Name](#P-ChuckDeviceController-Plugin-DashboardStatsItem-Name 'ChuckDeviceController.Plugin.DashboardStatsItem.Name')
    - [Value](#P-ChuckDeviceController-Plugin-DashboardStatsItem-Value 'ChuckDeviceController.Plugin.DashboardStatsItem.Value')
- [DashboardTile](#T-ChuckDeviceController-Plugin-DashboardTile 'ChuckDeviceController.Plugin.DashboardTile')
    - [#ctor(text,value,icon,controllerName,actionName)](#M-ChuckDeviceController-Plugin-DashboardTile-#ctor-System-String,System-String,System-String,System-String,System-String- 'ChuckDeviceController.Plugin.DashboardTile.#ctor(System.String,System.String,System.String,System.String,System.String)')
    - [ActionName](#P-ChuckDeviceController-Plugin-DashboardTile-ActionName 'ChuckDeviceController.Plugin.DashboardTile.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugin-DashboardTile-ControllerName 'ChuckDeviceController.Plugin.DashboardTile.ControllerName')
    - [Icon](#P-ChuckDeviceController-Plugin-DashboardTile-Icon 'ChuckDeviceController.Plugin.DashboardTile.Icon')
    - [Text](#P-ChuckDeviceController-Plugin-DashboardTile-Text 'ChuckDeviceController.Plugin.DashboardTile.Text')
    - [Value](#P-ChuckDeviceController-Plugin-DashboardTile-Value 'ChuckDeviceController.Plugin.DashboardTile.Value')
- [DatabaseConnectionState](#T-ChuckDeviceController-Plugin-DatabaseConnectionState 'ChuckDeviceController.Plugin.DatabaseConnectionState')
    - [Connected](#F-ChuckDeviceController-Plugin-DatabaseConnectionState-Connected 'ChuckDeviceController.Plugin.DatabaseConnectionState.Connected')
    - [Disconnected](#F-ChuckDeviceController-Plugin-DatabaseConnectionState-Disconnected 'ChuckDeviceController.Plugin.DatabaseConnectionState.Disconnected')
- [IDashboardStatsItem](#T-ChuckDeviceController-Plugin-IDashboardStatsItem 'ChuckDeviceController.Plugin.IDashboardStatsItem')
    - [IsHtml](#P-ChuckDeviceController-Plugin-IDashboardStatsItem-IsHtml 'ChuckDeviceController.Plugin.IDashboardStatsItem.IsHtml')
    - [Name](#P-ChuckDeviceController-Plugin-IDashboardStatsItem-Name 'ChuckDeviceController.Plugin.IDashboardStatsItem.Name')
    - [Value](#P-ChuckDeviceController-Plugin-IDashboardStatsItem-Value 'ChuckDeviceController.Plugin.IDashboardStatsItem.Value')
- [IDashboardTile](#T-ChuckDeviceController-Plugin-IDashboardTile 'ChuckDeviceController.Plugin.IDashboardTile')
    - [ActionName](#P-ChuckDeviceController-Plugin-IDashboardTile-ActionName 'ChuckDeviceController.Plugin.IDashboardTile.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugin-IDashboardTile-ControllerName 'ChuckDeviceController.Plugin.IDashboardTile.ControllerName')
    - [Icon](#P-ChuckDeviceController-Plugin-IDashboardTile-Icon 'ChuckDeviceController.Plugin.IDashboardTile.Icon')
    - [Text](#P-ChuckDeviceController-Plugin-IDashboardTile-Text 'ChuckDeviceController.Plugin.IDashboardTile.Text')
    - [Value](#P-ChuckDeviceController-Plugin-IDashboardTile-Value 'ChuckDeviceController.Plugin.IDashboardTile.Value')
- [IDatabaseEvents](#T-ChuckDeviceController-Plugin-IDatabaseEvents 'ChuckDeviceController.Plugin.IDatabaseEvents')
    - [OnEntityAdded\`\`1(entity)](#M-ChuckDeviceController-Plugin-IDatabaseEvents-OnEntityAdded``1-``0- 'ChuckDeviceController.Plugin.IDatabaseEvents.OnEntityAdded``1(``0)')
    - [OnEntityDeleted\`\`1(entity)](#M-ChuckDeviceController-Plugin-IDatabaseEvents-OnEntityDeleted``1-``0- 'ChuckDeviceController.Plugin.IDatabaseEvents.OnEntityDeleted``1(``0)')
    - [OnEntityModified\`\`1(oldEntity,newEntity)](#M-ChuckDeviceController-Plugin-IDatabaseEvents-OnEntityModified``1-``0,``0- 'ChuckDeviceController.Plugin.IDatabaseEvents.OnEntityModified``1(``0,``0)')
    - [OnStateChanged(state)](#M-ChuckDeviceController-Plugin-IDatabaseEvents-OnStateChanged-ChuckDeviceController-Plugin-DatabaseConnectionState- 'ChuckDeviceController.Plugin.IDatabaseEvents.OnStateChanged(ChuckDeviceController.Plugin.DatabaseConnectionState)')
- [IDatabaseHost](#T-ChuckDeviceController-Plugin-IDatabaseHost 'ChuckDeviceController.Plugin.IDatabaseHost')
    - [GetByIdAsync\`\`2(id)](#M-ChuckDeviceController-Plugin-IDatabaseHost-GetByIdAsync``2-``1- 'ChuckDeviceController.Plugin.IDatabaseHost.GetByIdAsync``2(``1)')
    - [GetListAsync\`\`1()](#M-ChuckDeviceController-Plugin-IDatabaseHost-GetListAsync``1 'ChuckDeviceController.Plugin.IDatabaseHost.GetListAsync``1')
- [IJobControllerServiceEvents](#T-ChuckDeviceController-Plugin-IJobControllerServiceEvents 'ChuckDeviceController.Plugin.IJobControllerServiceEvents')
- [IJobControllerServiceHost](#T-ChuckDeviceController-Plugin-IJobControllerServiceHost 'ChuckDeviceController.Plugin.IJobControllerServiceHost')
    - [AddJobControllerAsync(name,controller)](#M-ChuckDeviceController-Plugin-IJobControllerServiceHost-AddJobControllerAsync-System-String,ChuckDeviceController-Common-Jobs-IJobController- 'ChuckDeviceController.Plugin.IJobControllerServiceHost.AddJobControllerAsync(System.String,ChuckDeviceController.Common.Jobs.IJobController)')
    - [AssignDeviceToJobControllerAsync(device,jobControllerName)](#M-ChuckDeviceController-Plugin-IJobControllerServiceHost-AssignDeviceToJobControllerAsync-ChuckDeviceController-Common-Data-Contracts-IDevice,System-String- 'ChuckDeviceController.Plugin.IJobControllerServiceHost.AssignDeviceToJobControllerAsync(ChuckDeviceController.Common.Data.Contracts.IDevice,System.String)')
- [ILocalizationHost](#T-ChuckDeviceController-Plugin-ILocalizationHost 'ChuckDeviceController.Plugin.ILocalizationHost')
    - [CountryCode](#P-ChuckDeviceController-Plugin-ILocalizationHost-CountryCode 'ChuckDeviceController.Plugin.ILocalizationHost.CountryCode')
    - [CurrentCulture](#P-ChuckDeviceController-Plugin-ILocalizationHost-CurrentCulture 'ChuckDeviceController.Plugin.ILocalizationHost.CurrentCulture')
    - [GetAlignmentName(alignmentTypeId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetAlignmentName-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetAlignmentName(System.UInt32)')
    - [GetCharacterCategoryName(characterCategoryId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetCharacterCategoryName-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetCharacterCategoryName(System.UInt32)')
    - [GetCostumeName(costumeId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetCostumeName-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetCostumeName(System.UInt32)')
    - [GetEvolutionName(evolutionId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetEvolutionName-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetEvolutionName(System.UInt32)')
    - [GetFormName(formId,includeNormal)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetFormName-System-UInt32,System-Boolean- 'ChuckDeviceController.Plugin.ILocalizationHost.GetFormName(System.UInt32,System.Boolean)')
    - [GetGruntType(invasionCharacterId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetGruntType-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetGruntType(System.UInt32)')
    - [GetItem(itemId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetItem-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetItem(System.UInt32)')
    - [GetMoveName(moveId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetMoveName-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetMoveName(System.UInt32)')
    - [GetPokemonName(pokemonId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetPokemonName-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetPokemonName(System.UInt32)')
    - [GetThrowName(throwTypeId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetThrowName-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetThrowName(System.UInt32)')
    - [GetWeather(weatherConditionId)](#M-ChuckDeviceController-Plugin-ILocalizationHost-GetWeather-System-UInt32- 'ChuckDeviceController.Plugin.ILocalizationHost.GetWeather(System.UInt32)')
    - [SetLocale(locale)](#M-ChuckDeviceController-Plugin-ILocalizationHost-SetLocale-System-String- 'ChuckDeviceController.Plugin.ILocalizationHost.SetLocale(System.String)')
    - [Translate(key)](#M-ChuckDeviceController-Plugin-ILocalizationHost-Translate-System-String- 'ChuckDeviceController.Plugin.ILocalizationHost.Translate(System.String)')
    - [Translate(keyWithArgs,args)](#M-ChuckDeviceController-Plugin-ILocalizationHost-Translate-System-String,System-Object[]- 'ChuckDeviceController.Plugin.ILocalizationHost.Translate(System.String,System.Object[])')
- [ILoggingHost](#T-ChuckDeviceController-Plugin-ILoggingHost 'ChuckDeviceController.Plugin.ILoggingHost')
    - [LogException(ex)](#M-ChuckDeviceController-Plugin-ILoggingHost-LogException-System-Exception- 'ChuckDeviceController.Plugin.ILoggingHost.LogException(System.Exception)')
    - [LogMessage(text,args)](#M-ChuckDeviceController-Plugin-ILoggingHost-LogMessage-System-String,System-Object[]- 'ChuckDeviceController.Plugin.ILoggingHost.LogMessage(System.String,System.Object[])')
- [IMetadata](#T-ChuckDeviceController-Plugin-IMetadata 'ChuckDeviceController.Plugin.IMetadata')
    - [Author](#P-ChuckDeviceController-Plugin-IMetadata-Author 'ChuckDeviceController.Plugin.IMetadata.Author')
    - [Description](#P-ChuckDeviceController-Plugin-IMetadata-Description 'ChuckDeviceController.Plugin.IMetadata.Description')
    - [Name](#P-ChuckDeviceController-Plugin-IMetadata-Name 'ChuckDeviceController.Plugin.IMetadata.Name')
    - [Version](#P-ChuckDeviceController-Plugin-IMetadata-Version 'ChuckDeviceController.Plugin.IMetadata.Version')
- [INavbarHeader](#T-ChuckDeviceController-Plugin-INavbarHeader 'ChuckDeviceController.Plugin.INavbarHeader')
    - [ActionName](#P-ChuckDeviceController-Plugin-INavbarHeader-ActionName 'ChuckDeviceController.Plugin.INavbarHeader.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugin-INavbarHeader-ControllerName 'ChuckDeviceController.Plugin.INavbarHeader.ControllerName')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-INavbarHeader-DisplayIndex 'ChuckDeviceController.Plugin.INavbarHeader.DisplayIndex')
    - [Icon](#P-ChuckDeviceController-Plugin-INavbarHeader-Icon 'ChuckDeviceController.Plugin.INavbarHeader.Icon')
    - [IsDisabled](#P-ChuckDeviceController-Plugin-INavbarHeader-IsDisabled 'ChuckDeviceController.Plugin.INavbarHeader.IsDisabled')
    - [Text](#P-ChuckDeviceController-Plugin-INavbarHeader-Text 'ChuckDeviceController.Plugin.INavbarHeader.Text')
- [IPlugin](#T-ChuckDeviceController-Plugin-IPlugin 'ChuckDeviceController.Plugin.IPlugin')
- [IPluginBootstrapper](#T-ChuckDeviceController-Plugin-Services-IPluginBootstrapper 'ChuckDeviceController.Plugin.Services.IPluginBootstrapper')
    - [Bootstrap(services)](#M-ChuckDeviceController-Plugin-Services-IPluginBootstrapper-Bootstrap-Microsoft-Extensions-DependencyInjection-IServiceCollection- 'ChuckDeviceController.Plugin.Services.IPluginBootstrapper.Bootstrap(Microsoft.Extensions.DependencyInjection.IServiceCollection)')
- [IPluginBootstrapperServiceAttribute](#T-ChuckDeviceController-Plugin-Services-IPluginBootstrapperServiceAttribute 'ChuckDeviceController.Plugin.Services.IPluginBootstrapperServiceAttribute')
    - [ProxyType](#P-ChuckDeviceController-Plugin-Services-IPluginBootstrapperServiceAttribute-ProxyType 'ChuckDeviceController.Plugin.Services.IPluginBootstrapperServiceAttribute.ProxyType')
    - [ServiceType](#P-ChuckDeviceController-Plugin-Services-IPluginBootstrapperServiceAttribute-ServiceType 'ChuckDeviceController.Plugin.Services.IPluginBootstrapperServiceAttribute.ServiceType')
- [IPluginEvents](#T-ChuckDeviceController-Plugin-IPluginEvents 'ChuckDeviceController.Plugin.IPluginEvents')
    - [OnLoad()](#M-ChuckDeviceController-Plugin-IPluginEvents-OnLoad 'ChuckDeviceController.Plugin.IPluginEvents.OnLoad')
    - [OnReload()](#M-ChuckDeviceController-Plugin-IPluginEvents-OnReload 'ChuckDeviceController.Plugin.IPluginEvents.OnReload')
    - [OnRemove()](#M-ChuckDeviceController-Plugin-IPluginEvents-OnRemove 'ChuckDeviceController.Plugin.IPluginEvents.OnRemove')
    - [OnStateChanged(state)](#M-ChuckDeviceController-Plugin-IPluginEvents-OnStateChanged-ChuckDeviceController-Common-Data-PluginState- 'ChuckDeviceController.Plugin.IPluginEvents.OnStateChanged(ChuckDeviceController.Common.Data.PluginState)')
    - [OnStop()](#M-ChuckDeviceController-Plugin-IPluginEvents-OnStop 'ChuckDeviceController.Plugin.IPluginEvents.OnStop')
- [IPluginServiceAttribute](#T-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute 'ChuckDeviceController.Plugin.Services.IPluginServiceAttribute')
    - [Lifetime](#P-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute-Lifetime 'ChuckDeviceController.Plugin.Services.IPluginServiceAttribute.Lifetime')
    - [Provider](#P-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute-Provider 'ChuckDeviceController.Plugin.Services.IPluginServiceAttribute.Provider')
    - [ProxyType](#P-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute-ProxyType 'ChuckDeviceController.Plugin.Services.IPluginServiceAttribute.ProxyType')
    - [ServiceType](#P-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute-ServiceType 'ChuckDeviceController.Plugin.Services.IPluginServiceAttribute.ServiceType')
- [IRepository\`2](#T-ChuckDeviceController-Plugin-Data-IRepository`2 'ChuckDeviceController.Plugin.Data.IRepository`2')
    - [GetByIdAsync(id)](#M-ChuckDeviceController-Plugin-Data-IRepository`2-GetByIdAsync-`1- 'ChuckDeviceController.Plugin.Data.IRepository`2.GetByIdAsync(`1)')
    - [GetListAsync()](#M-ChuckDeviceController-Plugin-Data-IRepository`2-GetListAsync 'ChuckDeviceController.Plugin.Data.IRepository`2.GetListAsync')
- [IUiEvents](#T-ChuckDeviceController-Plugin-IUiEvents 'ChuckDeviceController.Plugin.IUiEvents')
- [IUiHost](#T-ChuckDeviceController-Plugin-IUiHost 'ChuckDeviceController.Plugin.IUiHost')
    - [DashboardStatsItems](#P-ChuckDeviceController-Plugin-IUiHost-DashboardStatsItems 'ChuckDeviceController.Plugin.IUiHost.DashboardStatsItems')
    - [DashboardTiles](#P-ChuckDeviceController-Plugin-IUiHost-DashboardTiles 'ChuckDeviceController.Plugin.IUiHost.DashboardTiles')
    - [NavbarHeaders](#P-ChuckDeviceController-Plugin-IUiHost-NavbarHeaders 'ChuckDeviceController.Plugin.IUiHost.NavbarHeaders')
    - [AddDashboardStatisticAsync(stat)](#M-ChuckDeviceController-Plugin-IUiHost-AddDashboardStatisticAsync-ChuckDeviceController-Plugin-IDashboardStatsItem- 'ChuckDeviceController.Plugin.IUiHost.AddDashboardStatisticAsync(ChuckDeviceController.Plugin.IDashboardStatsItem)')
    - [AddDashboardStatisticsAsync(stats)](#M-ChuckDeviceController-Plugin-IUiHost-AddDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardStatsItem}- 'ChuckDeviceController.Plugin.IUiHost.AddDashboardStatisticsAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardStatsItem})')
    - [AddDashboardTileAsync(tile)](#M-ChuckDeviceController-Plugin-IUiHost-AddDashboardTileAsync-ChuckDeviceController-Plugin-IDashboardTile- 'ChuckDeviceController.Plugin.IUiHost.AddDashboardTileAsync(ChuckDeviceController.Plugin.IDashboardTile)')
    - [AddDashboardTilesAsync(tiles)](#M-ChuckDeviceController-Plugin-IUiHost-AddDashboardTilesAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardTile}- 'ChuckDeviceController.Plugin.IUiHost.AddDashboardTilesAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardTile})')
    - [AddNavbarHeaderAsync(header)](#M-ChuckDeviceController-Plugin-IUiHost-AddNavbarHeaderAsync-ChuckDeviceController-Plugin-NavbarHeader- 'ChuckDeviceController.Plugin.IUiHost.AddNavbarHeaderAsync(ChuckDeviceController.Plugin.NavbarHeader)')
    - [AddNavbarHeadersAsync(headers)](#M-ChuckDeviceController-Plugin-IUiHost-AddNavbarHeadersAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-NavbarHeader}- 'ChuckDeviceController.Plugin.IUiHost.AddNavbarHeadersAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.NavbarHeader})')
    - [UpdateDashboardStatisticAsync(stat)](#M-ChuckDeviceController-Plugin-IUiHost-UpdateDashboardStatisticAsync-ChuckDeviceController-Plugin-IDashboardStatsItem- 'ChuckDeviceController.Plugin.IUiHost.UpdateDashboardStatisticAsync(ChuckDeviceController.Plugin.IDashboardStatsItem)')
    - [UpdateDashboardStatisticsAsync(stats)](#M-ChuckDeviceController-Plugin-IUiHost-UpdateDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardStatsItem}- 'ChuckDeviceController.Plugin.IUiHost.UpdateDashboardStatisticsAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardStatsItem})')
- [IWebPlugin](#T-ChuckDeviceController-Plugin-IWebPlugin 'ChuckDeviceController.Plugin.IWebPlugin')
    - [Configure(appBuilder)](#M-ChuckDeviceController-Plugin-IWebPlugin-Configure-Microsoft-AspNetCore-Builder-WebApplication- 'ChuckDeviceController.Plugin.IWebPlugin.Configure(Microsoft.AspNetCore.Builder.WebApplication)')
    - [ConfigureServices(services)](#M-ChuckDeviceController-Plugin-IWebPlugin-ConfigureServices-Microsoft-Extensions-DependencyInjection-IServiceCollection- 'ChuckDeviceController.Plugin.IWebPlugin.ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)')
- [NavbarHeader](#T-ChuckDeviceController-Plugin-NavbarHeader 'ChuckDeviceController.Plugin.NavbarHeader')
    - [#ctor()](#M-ChuckDeviceController-Plugin-NavbarHeader-#ctor 'ChuckDeviceController.Plugin.NavbarHeader.#ctor')
    - [#ctor(text,controllerName,actionName,icon,displayIndex,isDropdown,dropdownItems,isDisabled,isSeparator)](#M-ChuckDeviceController-Plugin-NavbarHeader-#ctor-System-String,System-String,System-String,System-String,System-UInt32,System-Boolean,System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-NavbarHeader},System-Boolean,System-Boolean- 'ChuckDeviceController.Plugin.NavbarHeader.#ctor(System.String,System.String,System.String,System.String,System.UInt32,System.Boolean,System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.NavbarHeader},System.Boolean,System.Boolean)')
    - [ActionName](#P-ChuckDeviceController-Plugin-NavbarHeader-ActionName 'ChuckDeviceController.Plugin.NavbarHeader.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugin-NavbarHeader-ControllerName 'ChuckDeviceController.Plugin.NavbarHeader.ControllerName')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-NavbarHeader-DisplayIndex 'ChuckDeviceController.Plugin.NavbarHeader.DisplayIndex')
    - [DropdownItems](#P-ChuckDeviceController-Plugin-NavbarHeader-DropdownItems 'ChuckDeviceController.Plugin.NavbarHeader.DropdownItems')
    - [Icon](#P-ChuckDeviceController-Plugin-NavbarHeader-Icon 'ChuckDeviceController.Plugin.NavbarHeader.Icon')
    - [IsDisabled](#P-ChuckDeviceController-Plugin-NavbarHeader-IsDisabled 'ChuckDeviceController.Plugin.NavbarHeader.IsDisabled')
    - [IsDropdown](#P-ChuckDeviceController-Plugin-NavbarHeader-IsDropdown 'ChuckDeviceController.Plugin.NavbarHeader.IsDropdown')
    - [IsSeparator](#P-ChuckDeviceController-Plugin-NavbarHeader-IsSeparator 'ChuckDeviceController.Plugin.NavbarHeader.IsSeparator')
    - [Text](#P-ChuckDeviceController-Plugin-NavbarHeader-Text 'ChuckDeviceController.Plugin.NavbarHeader.Text')
- [PluginBootstrapperAttribute](#T-ChuckDeviceController-Plugin-Services-PluginBootstrapperAttribute 'ChuckDeviceController.Plugin.Services.PluginBootstrapperAttribute')
    - [#ctor(pluginType)](#M-ChuckDeviceController-Plugin-Services-PluginBootstrapperAttribute-#ctor-System-Type- 'ChuckDeviceController.Plugin.Services.PluginBootstrapperAttribute.#ctor(System.Type)')
    - [PluginType](#P-ChuckDeviceController-Plugin-Services-PluginBootstrapperAttribute-PluginType 'ChuckDeviceController.Plugin.Services.PluginBootstrapperAttribute.PluginType')
- [PluginBootstrapperServiceAttribute](#T-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute 'ChuckDeviceController.Plugin.Services.PluginBootstrapperServiceAttribute')
    - [#ctor(serviceType)](#M-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute-#ctor-System-Type- 'ChuckDeviceController.Plugin.Services.PluginBootstrapperServiceAttribute.#ctor(System.Type)')
    - [#ctor(serviceType,proxyType)](#M-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute-#ctor-System-Type,System-Type- 'ChuckDeviceController.Plugin.Services.PluginBootstrapperServiceAttribute.#ctor(System.Type,System.Type)')
    - [ProxyType](#P-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute-ProxyType 'ChuckDeviceController.Plugin.Services.PluginBootstrapperServiceAttribute.ProxyType')
    - [ServiceType](#P-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute-ServiceType 'ChuckDeviceController.Plugin.Services.PluginBootstrapperServiceAttribute.ServiceType')
- [PluginPermissions](#T-ChuckDeviceController-Plugin-PluginPermissions 'ChuckDeviceController.Plugin.PluginPermissions')
    - [AddControllers](#F-ChuckDeviceController-Plugin-PluginPermissions-AddControllers 'ChuckDeviceController.Plugin.PluginPermissions.AddControllers')
    - [AddInstances](#F-ChuckDeviceController-Plugin-PluginPermissions-AddInstances 'ChuckDeviceController.Plugin.PluginPermissions.AddInstances')
    - [AddJobControllers](#F-ChuckDeviceController-Plugin-PluginPermissions-AddJobControllers 'ChuckDeviceController.Plugin.PluginPermissions.AddJobControllers')
    - [All](#F-ChuckDeviceController-Plugin-PluginPermissions-All 'ChuckDeviceController.Plugin.PluginPermissions.All')
    - [DeleteDatabase](#F-ChuckDeviceController-Plugin-PluginPermissions-DeleteDatabase 'ChuckDeviceController.Plugin.PluginPermissions.DeleteDatabase')
    - [None](#F-ChuckDeviceController-Plugin-PluginPermissions-None 'ChuckDeviceController.Plugin.PluginPermissions.None')
    - [ReadDatabase](#F-ChuckDeviceController-Plugin-PluginPermissions-ReadDatabase 'ChuckDeviceController.Plugin.PluginPermissions.ReadDatabase')
    - [WriteDatabase](#F-ChuckDeviceController-Plugin-PluginPermissions-WriteDatabase 'ChuckDeviceController.Plugin.PluginPermissions.WriteDatabase')
- [PluginPermissionsAttribute](#T-ChuckDeviceController-Plugin-PluginPermissionsAttribute 'ChuckDeviceController.Plugin.PluginPermissionsAttribute')
    - [#ctor(permissions)](#M-ChuckDeviceController-Plugin-PluginPermissionsAttribute-#ctor-ChuckDeviceController-Plugin-PluginPermissions- 'ChuckDeviceController.Plugin.PluginPermissionsAttribute.#ctor(ChuckDeviceController.Plugin.PluginPermissions)')
    - [Permissions](#P-ChuckDeviceController-Plugin-PluginPermissionsAttribute-Permissions 'ChuckDeviceController.Plugin.PluginPermissionsAttribute.Permissions')
- [PluginServiceAttribute](#T-ChuckDeviceController-Plugin-Services-PluginServiceAttribute 'ChuckDeviceController.Plugin.Services.PluginServiceAttribute')
    - [#ctor()](#M-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-#ctor 'ChuckDeviceController.Plugin.Services.PluginServiceAttribute.#ctor')
    - [#ctor(serviceType,proxyType,provider,lifetime)](#M-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-#ctor-System-Type,System-Type,ChuckDeviceController-Plugin-Services-PluginServiceProvider,Microsoft-Extensions-DependencyInjection-ServiceLifetime- 'ChuckDeviceController.Plugin.Services.PluginServiceAttribute.#ctor(System.Type,System.Type,ChuckDeviceController.Plugin.Services.PluginServiceProvider,Microsoft.Extensions.DependencyInjection.ServiceLifetime)')
    - [Lifetime](#P-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-Lifetime 'ChuckDeviceController.Plugin.Services.PluginServiceAttribute.Lifetime')
    - [Provider](#P-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-Provider 'ChuckDeviceController.Plugin.Services.PluginServiceAttribute.Provider')
    - [ProxyType](#P-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-ProxyType 'ChuckDeviceController.Plugin.Services.PluginServiceAttribute.ProxyType')
    - [ServiceType](#P-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-ServiceType 'ChuckDeviceController.Plugin.Services.PluginServiceAttribute.ServiceType')
- [PluginServiceProvider](#T-ChuckDeviceController-Plugin-Services-PluginServiceProvider 'ChuckDeviceController.Plugin.Services.PluginServiceProvider')
    - [Host](#F-ChuckDeviceController-Plugin-Services-PluginServiceProvider-Host 'ChuckDeviceController.Plugin.Services.PluginServiceProvider.Host')
    - [Plugin](#F-ChuckDeviceController-Plugin-Services-PluginServiceProvider-Plugin 'ChuckDeviceController.Plugin.Services.PluginServiceProvider.Plugin')
- [StaticFilesLocation](#T-ChuckDeviceController-Plugin-StaticFilesLocation 'ChuckDeviceController.Plugin.StaticFilesLocation')
    - [External](#F-ChuckDeviceController-Plugin-StaticFilesLocation-External 'ChuckDeviceController.Plugin.StaticFilesLocation.External')
    - [None](#F-ChuckDeviceController-Plugin-StaticFilesLocation-None 'ChuckDeviceController.Plugin.StaticFilesLocation.None')
    - [Resources](#F-ChuckDeviceController-Plugin-StaticFilesLocation-Resources 'ChuckDeviceController.Plugin.StaticFilesLocation.Resources')
- [StaticFilesLocationAttribute](#T-ChuckDeviceController-Plugin-StaticFilesLocationAttribute 'ChuckDeviceController.Plugin.StaticFilesLocationAttribute')
    - [#ctor(location)](#M-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-#ctor-ChuckDeviceController-Plugin-StaticFilesLocation- 'ChuckDeviceController.Plugin.StaticFilesLocationAttribute.#ctor(ChuckDeviceController.Plugin.StaticFilesLocation)')
    - [Location](#P-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-Location 'ChuckDeviceController.Plugin.StaticFilesLocationAttribute.Location')

<a name='T-ChuckDeviceController-Plugin-DashboardStatsItem'></a>
## DashboardStatsItem `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Dashboard statistics item for displaying information
on the front page.

<a name='M-ChuckDeviceController-Plugin-DashboardStatsItem-#ctor-System-String,System-String,System-Boolean-'></a>
### #ctor(name,value,isHtml) `constructor`

##### Summary

Instantiates a new dashboard statistics item using
the provided property values.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Name of the statistic. |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Value of the statistic. |
| isHtml | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Whether the name or value contain raw HTML. |

<a name='P-ChuckDeviceController-Plugin-DashboardStatsItem-IsHtml'></a>
### IsHtml `property`

##### Summary

Gets or sets a value determining whether the name
and value properties include raw HTML or not.

<a name='P-ChuckDeviceController-Plugin-DashboardStatsItem-Name'></a>
### Name `property`

##### Summary

Gets or sets the name or title of the statistic.

<a name='P-ChuckDeviceController-Plugin-DashboardStatsItem-Value'></a>
### Value `property`

##### Summary

Gets or sets the value of the statistic.

<a name='T-ChuckDeviceController-Plugin-DashboardTile'></a>
## DashboardTile `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary



<a name='M-ChuckDeviceController-Plugin-DashboardTile-#ctor-System-String,System-String,System-String,System-String,System-String-'></a>
### #ctor(text,value,icon,controllerName,actionName) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| text | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| icon | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| controllerName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| actionName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='P-ChuckDeviceController-Plugin-DashboardTile-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the navbar header is clicked.

<a name='P-ChuckDeviceController-Plugin-DashboardTile-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to when the tile is clicked.

<a name='P-ChuckDeviceController-Plugin-DashboardTile-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the Fontawesome icon to display.

<a name='P-ChuckDeviceController-Plugin-DashboardTile-Text'></a>
### Text `property`

##### Summary

Gets or sets the text displayed for the dashboard tile.

<a name='P-ChuckDeviceController-Plugin-DashboardTile-Value'></a>
### Value `property`

##### Summary

Gets or sets the value for the dashboard tile.

<a name='T-ChuckDeviceController-Plugin-DatabaseConnectionState'></a>
## DatabaseConnectionState `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Enumeration of possible database connection states.

<a name='F-ChuckDeviceController-Plugin-DatabaseConnectionState-Connected'></a>
### Connected `constants`

##### Summary

Database is in the connected state.

<a name='F-ChuckDeviceController-Plugin-DatabaseConnectionState-Disconnected'></a>
### Disconnected `constants`

##### Summary

Database is in the disconnected state.

<a name='T-ChuckDeviceController-Plugin-IDashboardStatsItem'></a>
## IDashboardStatsItem `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Dashboard statistics item for displaying information
on the front page.

<a name='P-ChuckDeviceController-Plugin-IDashboardStatsItem-IsHtml'></a>
### IsHtml `property`

##### Summary

Gets or sets a value determining whether the name
and value properties include raw HTML or not.

<a name='P-ChuckDeviceController-Plugin-IDashboardStatsItem-Name'></a>
### Name `property`

##### Summary

Gets or sets the name or title of the statistic.

<a name='P-ChuckDeviceController-Plugin-IDashboardStatsItem-Value'></a>
### Value `property`

##### Summary

Gets or sets the value of the statistic.

<a name='T-ChuckDeviceController-Plugin-IDashboardTile'></a>
## IDashboardTile `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary



<a name='P-ChuckDeviceController-Plugin-IDashboardTile-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the navbar header is clicked.

<a name='P-ChuckDeviceController-Plugin-IDashboardTile-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to when the tile is clicked.

<a name='P-ChuckDeviceController-Plugin-IDashboardTile-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the Fontawesome icon to display.

<a name='P-ChuckDeviceController-Plugin-IDashboardTile-Text'></a>
### Text `property`

##### Summary

Gets or sets the text displayed for the dashboard tile.

<a name='P-ChuckDeviceController-Plugin-IDashboardTile-Value'></a>
### Value `property`

##### Summary

Gets or sets the value for the dashboard tile.

<a name='T-ChuckDeviceController-Plugin-IDatabaseEvents'></a>
## IDatabaseEvents `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Provides delegates of database related events from
the host application.

<a name='M-ChuckDeviceController-Plugin-IDatabaseEvents-OnEntityAdded``1-``0-'></a>
### OnEntityAdded\`\`1(entity) `method`

##### Summary

Called when an entity has been added to the database by
the host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| entity | [\`\`0](#T-``0 '``0') | The entity that was added. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Data entity type that was added. |

<a name='M-ChuckDeviceController-Plugin-IDatabaseEvents-OnEntityDeleted``1-``0-'></a>
### OnEntityDeleted\`\`1(entity) `method`

##### Summary

Called when an entity has been deleted in the database by
the host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| entity | [\`\`0](#T-``0 '``0') | The entity that was deleted. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Data entity type that was deleted. |

<a name='M-ChuckDeviceController-Plugin-IDatabaseEvents-OnEntityModified``1-``0,``0-'></a>
### OnEntityModified\`\`1(oldEntity,newEntity) `method`

##### Summary

Called when an entity has been modified in the database by
the host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| oldEntity | [\`\`0](#T-``0 '``0') | The entity's previous version. |
| newEntity | [\`\`0](#T-``0 '``0') | The entity that was modified. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Data entity type that was modified. |

<a name='M-ChuckDeviceController-Plugin-IDatabaseEvents-OnStateChanged-ChuckDeviceController-Plugin-DatabaseConnectionState-'></a>
### OnStateChanged(state) `method`

##### Summary

Called when the state of the database has changed.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| state | [ChuckDeviceController.Plugin.DatabaseConnectionState](#T-ChuckDeviceController-Plugin-DatabaseConnectionState 'ChuckDeviceController.Plugin.DatabaseConnectionState') | Current state of the database connection. |

<a name='T-ChuckDeviceController-Plugin-IDatabaseHost'></a>
## IDatabaseHost `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Plugin host handler contract used to interact with the database entities.

<a name='M-ChuckDeviceController-Plugin-IDatabaseHost-GetByIdAsync``2-``1-'></a>
### GetByIdAsync\`\`2(id) `method`

##### Summary

Gets a database entity by primary key.

##### Returns

Returns a database entity.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [\`\`1](#T-``1 '``1') | Primary key of the database entity. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Database entity contract type. |
| TId | Database entity primary key type. |

<a name='M-ChuckDeviceController-Plugin-IDatabaseHost-GetListAsync``1'></a>
### GetListAsync\`\`1() `method`

##### Summary

Gets a list of database entities.

##### Returns

Returns a list of database entities.

##### Parameters

This method has no parameters.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Database entity contract type. |

<a name='T-ChuckDeviceController-Plugin-IJobControllerServiceEvents'></a>
## IJobControllerServiceEvents `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Job controller service related events that have occurred
in the host application.

<a name='T-ChuckDeviceController-Plugin-IJobControllerServiceHost'></a>
## IJobControllerServiceHost `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Plugin host handler contract used to interact with and manage the
job controller service.

<a name='M-ChuckDeviceController-Plugin-IJobControllerServiceHost-AddJobControllerAsync-System-String,ChuckDeviceController-Common-Jobs-IJobController-'></a>
### AddJobControllerAsync(name,controller) `method`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| controller | [ChuckDeviceController.Common.Jobs.IJobController](#T-ChuckDeviceController-Common-Jobs-IJobController 'ChuckDeviceController.Common.Jobs.IJobController') |  |

<a name='M-ChuckDeviceController-Plugin-IJobControllerServiceHost-AssignDeviceToJobControllerAsync-ChuckDeviceController-Common-Data-Contracts-IDevice,System-String-'></a>
### AssignDeviceToJobControllerAsync(device,jobControllerName) `method`

##### Summary

Assigns the specified device to a specific job controller
instance by name.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| device | [ChuckDeviceController.Common.Data.Contracts.IDevice](#T-ChuckDeviceController-Common-Data-Contracts-IDevice 'ChuckDeviceController.Common.Data.Contracts.IDevice') | Device entity. |
| jobControllerName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Job controller instance name. |

<a name='T-ChuckDeviceController-Plugin-ILocalizationHost'></a>
## ILocalizationHost `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Plugin host handler contract used to translate strings.

<a name='P-ChuckDeviceController-Plugin-ILocalizationHost-CountryCode'></a>
### CountryCode `property`

##### Summary

Gets the two letter ISO country code for the currently set localization.

<a name='P-ChuckDeviceController-Plugin-ILocalizationHost-CurrentCulture'></a>
### CurrentCulture `property`

##### Summary

Gets or sets the current culture localization to use.

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetAlignmentName-System-UInt32-'></a>
### GetAlignmentName(alignmentTypeId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| alignmentTypeId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetCharacterCategoryName-System-UInt32-'></a>
### GetCharacterCategoryName(characterCategoryId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| characterCategoryId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetCostumeName-System-UInt32-'></a>
### GetCostumeName(costumeId) `method`

##### Summary

Translate a Pokemon costume id to name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| costumeId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Costume ID to translate to name. |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetEvolutionName-System-UInt32-'></a>
### GetEvolutionName(evolutionId) `method`

##### Summary

Translate a Pokemon evolution id to name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| evolutionId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Evolution ID to translate to name. |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetFormName-System-UInt32,System-Boolean-'></a>
### GetFormName(formId,includeNormal) `method`

##### Summary

Translate a Pokemon form id to name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| formId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Form ID to translate to name. |
| includeNormal | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Include 'Normal' form name or not. |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetGruntType-System-UInt32-'></a>
### GetGruntType(invasionCharacterId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| invasionCharacterId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetItem-System-UInt32-'></a>
### GetItem(itemId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| itemId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetMoveName-System-UInt32-'></a>
### GetMoveName(moveId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| moveId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetPokemonName-System-UInt32-'></a>
### GetPokemonName(pokemonId) `method`

##### Summary

Translate a Pokemon id to name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| pokemonId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Pokemon ID to translate to name. |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetThrowName-System-UInt32-'></a>
### GetThrowName(throwTypeId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| throwTypeId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-GetWeather-System-UInt32-'></a>
### GetWeather(weatherConditionId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| weatherConditionId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-SetLocale-System-String-'></a>
### SetLocale(locale) `method`

##### Summary

Sets the country locale code to use for translations.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| locale | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Two letter ISO language name code. |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-Translate-System-String-'></a>
### Translate(key) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| key | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='M-ChuckDeviceController-Plugin-ILocalizationHost-Translate-System-String,System-Object[]-'></a>
### Translate(keyWithArgs,args) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| keyWithArgs | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| args | [System.Object[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object[] 'System.Object[]') |  |

<a name='T-ChuckDeviceController-Plugin-ILoggingHost'></a>
## ILoggingHost `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Plugin host handler for logging messages from plugins.

<a name='M-ChuckDeviceController-Plugin-ILoggingHost-LogException-System-Exception-'></a>
### LogException(ex) `method`

##### Summary

Log an exception that has been thrown to the
host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| ex | [System.Exception](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Exception 'System.Exception') | Exception that was thrown. |

<a name='M-ChuckDeviceController-Plugin-ILoggingHost-LogMessage-System-String,System-Object[]-'></a>
### LogMessage(text,args) `method`

##### Summary

Log a message to the host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| text | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Formatted log message string. |
| args | [System.Object[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object[] 'System.Object[]') | Arguments to parse with log message. |

<a name='T-ChuckDeviceController-Plugin-IMetadata'></a>
## IMetadata `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Plugin metadata details.

<a name='P-ChuckDeviceController-Plugin-IMetadata-Author'></a>
### Author `property`

##### Summary

Gets or sets the creator/author name that wrote the Plugin.

<a name='P-ChuckDeviceController-Plugin-IMetadata-Description'></a>
### Description `property`

##### Summary

Gets or sets the description about the Plugin.

<a name='P-ChuckDeviceController-Plugin-IMetadata-Name'></a>
### Name `property`

##### Summary

Gets or sets the name of the Plugin.

<a name='P-ChuckDeviceController-Plugin-IMetadata-Version'></a>
### Version `property`

##### Summary

Gets or sets the current version of the Plugin.

<a name='T-ChuckDeviceController-Plugin-INavbarHeader'></a>
## INavbarHeader `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Navigation bar header plugin contract.

<a name='P-ChuckDeviceController-Plugin-INavbarHeader-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the navbar header is clicked.

<a name='P-ChuckDeviceController-Plugin-INavbarHeader-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to.

<a name='P-ChuckDeviceController-Plugin-INavbarHeader-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the numeric display index order of
the navbar header in the list of navbar headers.

<a name='P-ChuckDeviceController-Plugin-INavbarHeader-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the FontAwesome v6 icon key to use for 
the navbar header. https://fontawesome.com/icons

<a name='P-ChuckDeviceController-Plugin-INavbarHeader-IsDisabled'></a>
### IsDisabled `property`

##### Summary

Gets or sets a value determining whether the
navbar header is disabled or not.

<a name='P-ChuckDeviceController-Plugin-INavbarHeader-Text'></a>
### Text `property`

##### Summary

Gets or sets the text to display for this navbar
header.

<a name='T-ChuckDeviceController-Plugin-IPlugin'></a>
## IPlugin `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Base Plugin interface contract all plugins are required to
inherit at a minimum.

<a name='T-ChuckDeviceController-Plugin-Services-IPluginBootstrapper'></a>
## IPluginBootstrapper `type`

##### Namespace

ChuckDeviceController.Plugin.Services

##### Summary

Register services from a separate class, aka 'ConfigureServices'

<a name='M-ChuckDeviceController-Plugin-Services-IPluginBootstrapper-Bootstrap-Microsoft-Extensions-DependencyInjection-IServiceCollection-'></a>
### Bootstrap(services) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| services | [Microsoft.Extensions.DependencyInjection.IServiceCollection](#T-Microsoft-Extensions-DependencyInjection-IServiceCollection 'Microsoft.Extensions.DependencyInjection.IServiceCollection') |  |

<a name='T-ChuckDeviceController-Plugin-Services-IPluginBootstrapperServiceAttribute'></a>
## IPluginBootstrapperServiceAttribute `type`

##### Namespace

ChuckDeviceController.Plugin.Services

##### Summary

Assigns fields and properties in a plugin assembly with registered
service implementations.

<a name='P-ChuckDeviceController-Plugin-Services-IPluginBootstrapperServiceAttribute-ProxyType'></a>
### ProxyType `property`

##### Summary

Gets or sets the bootstrap service implementation type.

<a name='P-ChuckDeviceController-Plugin-Services-IPluginBootstrapperServiceAttribute-ServiceType'></a>
### ServiceType `property`

##### Summary

Gets or sets the bootstrap service contract type.

<a name='T-ChuckDeviceController-Plugin-IPluginEvents'></a>
## IPluginEvents `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Provides delegates of plugin related events
from the host application.

<a name='M-ChuckDeviceController-Plugin-IPluginEvents-OnLoad'></a>
### OnLoad() `method`

##### Summary

Called when the plugin has been fully loaded
and initialized from the host application.

##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugin-IPluginEvents-OnReload'></a>
### OnReload() `method`

##### Summary

Called when the plugin has been reloaded
by the host application.

##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugin-IPluginEvents-OnRemove'></a>
### OnRemove() `method`

##### Summary

Called when the plugin has been removed by
the host application.

##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugin-IPluginEvents-OnStateChanged-ChuckDeviceController-Common-Data-PluginState-'></a>
### OnStateChanged(state) `method`

##### Summary

Called when the plugin's state has been
changed by the host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| state | [ChuckDeviceController.Common.Data.PluginState](#T-ChuckDeviceController-Common-Data-PluginState 'ChuckDeviceController.Common.Data.PluginState') | Plugin's current state |

<a name='M-ChuckDeviceController-Plugin-IPluginEvents-OnStop'></a>
### OnStop() `method`

##### Summary

Called when the plugin has been stopped by
the host application.

##### Parameters

This method has no parameters.

<a name='T-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute'></a>
## IPluginServiceAttribute `type`

##### Namespace

ChuckDeviceController.Plugin.Services

##### Summary

Contract for registering plugin service classes marked with
'PluginServiceAttribute' with the host application in order
to be used with dependency injection.

<a name='P-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute-Lifetime'></a>
### Lifetime `property`

##### Summary

Gets or sets the service lifetime for the plugin service.

<a name='P-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute-Provider'></a>
### Provider `property`

##### Summary

Gets or sets who provided the service.

<a name='P-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute-ProxyType'></a>
### ProxyType `property`

##### Summary

Gets or sets the service implementation type.

<a name='P-ChuckDeviceController-Plugin-Services-IPluginServiceAttribute-ServiceType'></a>
### ServiceType `property`

##### Summary

Gets or sets the Service contract type.

<a name='T-ChuckDeviceController-Plugin-Data-IRepository`2'></a>
## IRepository\`2 `type`

##### Namespace

ChuckDeviceController.Plugin.Data

##### Summary

Repository contract for specific database entity types.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TEntity | Database entity contract type. |
| TId | Database entity primary key type. |

<a name='M-ChuckDeviceController-Plugin-Data-IRepository`2-GetByIdAsync-`1-'></a>
### GetByIdAsync(id) `method`

##### Summary

Gets a database entity by primary key.

##### Returns

Returns a database entity.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [\`1](#T-`1 '`1') | Primary key of the database entity. |

<a name='M-ChuckDeviceController-Plugin-Data-IRepository`2-GetListAsync'></a>
### GetListAsync() `method`

##### Summary

Gets a list of database entities.

##### Returns

Returns a list of database entities.

##### Parameters

This method has no parameters.

<a name='T-ChuckDeviceController-Plugin-IUiEvents'></a>
## IUiEvents `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

UI related events that have occurred in
the host application.

<a name='T-ChuckDeviceController-Plugin-IUiHost'></a>
## IUiHost `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Plugin host handler for executing user interface operations.

<a name='P-ChuckDeviceController-Plugin-IUiHost-DashboardStatsItems'></a>
### DashboardStatsItems `property`

##### Summary

Gets a list of dashboard statistics registered by plugins.

<a name='P-ChuckDeviceController-Plugin-IUiHost-DashboardTiles'></a>
### DashboardTiles `property`

##### Summary

Gets a list of dashboard tiles registered by plugins.

<a name='P-ChuckDeviceController-Plugin-IUiHost-NavbarHeaders'></a>
### NavbarHeaders `property`

##### Summary

Gets a list of navbar headers registered by plugins.

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddDashboardStatisticAsync-ChuckDeviceController-Plugin-IDashboardStatsItem-'></a>
### AddDashboardStatisticAsync(stat) `method`

##### Summary

Adds a custom to the
dashboard front page.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stat | [ChuckDeviceController.Plugin.IDashboardStatsItem](#T-ChuckDeviceController-Plugin-IDashboardStatsItem 'ChuckDeviceController.Plugin.IDashboardStatsItem') | Dashboard statistics item to add. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardStatsItem}-'></a>
### AddDashboardStatisticsAsync(stats) `method`

##### Summary

Adds a list of items to
the dashboard front page.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stats | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardStatsItem}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardStatsItem}') | List of dashboard statistic items to add. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddDashboardTileAsync-ChuckDeviceController-Plugin-IDashboardTile-'></a>
### AddDashboardTileAsync(tile) `method`

##### Summary

Adds a statistic tile to the front page dashboard.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tile | [ChuckDeviceController.Plugin.IDashboardTile](#T-ChuckDeviceController-Plugin-IDashboardTile 'ChuckDeviceController.Plugin.IDashboardTile') | Dashboard statistics tile to add. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddDashboardTilesAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardTile}-'></a>
### AddDashboardTilesAsync(tiles) `method`

##### Summary

Adds a list of statistic tiles to the front page dashboard.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tiles | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardTile}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardTile}') | List of dashboard statistic tiles to add. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddNavbarHeaderAsync-ChuckDeviceController-Plugin-NavbarHeader-'></a>
### AddNavbarHeaderAsync(header) `method`

##### Summary

Adds a item to the main
application's Mvc navbar header.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| header | [ChuckDeviceController.Plugin.NavbarHeader](#T-ChuckDeviceController-Plugin-NavbarHeader 'ChuckDeviceController.Plugin.NavbarHeader') | Navbar to add. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddNavbarHeadersAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-NavbarHeader}-'></a>
### AddNavbarHeadersAsync(headers) `method`

##### Summary

Adds a list of items to the
main application's Mvc navbar header.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| headers | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.NavbarHeader}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.NavbarHeader}') | List of navbars to add. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-UpdateDashboardStatisticAsync-ChuckDeviceController-Plugin-IDashboardStatsItem-'></a>
### UpdateDashboardStatisticAsync(stat) `method`

##### Summary

Update an existing item
on the dashboard front page.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stat | [ChuckDeviceController.Plugin.IDashboardStatsItem](#T-ChuckDeviceController-Plugin-IDashboardStatsItem 'ChuckDeviceController.Plugin.IDashboardStatsItem') | Dashboard statistics item to update. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-UpdateDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardStatsItem}-'></a>
### UpdateDashboardStatisticsAsync(stats) `method`

##### Summary

Update a list of existing items
on the dashboard front page.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stats | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardStatsItem}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardStatsItem}') | List of dashboard statistic items to update. |

<a name='T-ChuckDeviceController-Plugin-IWebPlugin'></a>
## IWebPlugin `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Interface contract allowing Mvc services registration and configuration

<a name='M-ChuckDeviceController-Plugin-IWebPlugin-Configure-Microsoft-AspNetCore-Builder-WebApplication-'></a>
### Configure(appBuilder) `method`

##### Summary

Configures the application to set up middlewares, routing rules, etc.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| appBuilder | [Microsoft.AspNetCore.Builder.WebApplication](#T-Microsoft-AspNetCore-Builder-WebApplication 'Microsoft.AspNetCore.Builder.WebApplication') | Provides the mechanisms to configure an application's request pipeline. |

<a name='M-ChuckDeviceController-Plugin-IWebPlugin-ConfigureServices-Microsoft-Extensions-DependencyInjection-IServiceCollection-'></a>
### ConfigureServices(services) `method`

##### Summary

Register services into the IServiceCollection to use with Dependency Injection.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| services | [Microsoft.Extensions.DependencyInjection.IServiceCollection](#T-Microsoft-Extensions-DependencyInjection-IServiceCollection 'Microsoft.Extensions.DependencyInjection.IServiceCollection') | Specifies the contract for a collection of service descriptors. |

<a name='T-ChuckDeviceController-Plugin-NavbarHeader'></a>
## NavbarHeader `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Navigation bar header plugin contract implementation.

<a name='M-ChuckDeviceController-Plugin-NavbarHeader-#ctor'></a>
### #ctor() `constructor`

##### Summary

Instantiates a new navbar header instance using default 
property values.

##### Parameters

This constructor has no parameters.

<a name='M-ChuckDeviceController-Plugin-NavbarHeader-#ctor-System-String,System-String,System-String,System-String,System-UInt32,System-Boolean,System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-NavbarHeader},System-Boolean,System-Boolean-'></a>
### #ctor(text,controllerName,actionName,icon,displayIndex,isDropdown,dropdownItems,isDisabled,isSeparator) `constructor`

##### Summary

Instantiates a new navbar header instance using the specified
property values.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| text | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| controllerName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| actionName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| icon | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| displayIndex | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |
| isDropdown | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |
| dropdownItems | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.NavbarHeader}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.NavbarHeader}') |  |
| isDisabled | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |
| isSeparator | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the navbar header is clicked.

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to.

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the numeric display index order of
the navbar header in the list of navbar headers.

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-DropdownItems'></a>
### DropdownItems `property`

##### Summary

Gets or sets a list of navbar header dropdown items.

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the FontAwesome v6 icon key to use for 
the navbar header. https://fontawesome.com/icons

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-IsDisabled'></a>
### IsDisabled `property`

##### Summary

Gets or sets a value determining whether the
navbar header is disabled or not.

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-IsDropdown'></a>
### IsDropdown `property`

##### Summary

Gets or sets a value determining whether the navbar
header should be treated as a dropdown.

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-IsSeparator'></a>
### IsSeparator `property`

##### Summary

Gets or sets a value determining whether to insert a
separator instead of a dropdown item.

<a name='P-ChuckDeviceController-Plugin-NavbarHeader-Text'></a>
### Text `property`

##### Summary

Gets or sets the text to display for this navbar
header.

<a name='T-ChuckDeviceController-Plugin-Services-PluginBootstrapperAttribute'></a>
## PluginBootstrapperAttribute `type`

##### Namespace

ChuckDeviceController.Plugin.Services

##### Summary

Register services from a separate class, aka 'ConfigureServices'

<a name='M-ChuckDeviceController-Plugin-Services-PluginBootstrapperAttribute-#ctor-System-Type-'></a>
### #ctor(pluginType) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| pluginType | [System.Type](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Type 'System.Type') |  |

<a name='P-ChuckDeviceController-Plugin-Services-PluginBootstrapperAttribute-PluginType'></a>
### PluginType `property`

##### Summary

Gets or sets the plugin contract type.

<a name='T-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute'></a>
## PluginBootstrapperServiceAttribute `type`

##### Namespace

ChuckDeviceController.Plugin.Services

##### Summary

Assigns fields and properties in a plugin assembly with registered
service implementations.

<a name='M-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute-#ctor-System-Type-'></a>
### #ctor(serviceType) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| serviceType | [System.Type](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Type 'System.Type') |  |

<a name='M-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute-#ctor-System-Type,System-Type-'></a>
### #ctor(serviceType,proxyType) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| serviceType | [System.Type](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Type 'System.Type') |  |
| proxyType | [System.Type](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Type 'System.Type') |  |

<a name='P-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute-ProxyType'></a>
### ProxyType `property`

##### Summary

Gets or sets the bootstrap service implementation type.

<a name='P-ChuckDeviceController-Plugin-Services-PluginBootstrapperServiceAttribute-ServiceType'></a>
### ServiceType `property`

##### Summary

Gets or sets the bootstrap service contract type.

<a name='T-ChuckDeviceController-Plugin-PluginPermissions'></a>
## PluginPermissions `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Enumeration of available permissions a plugin can request.

<a name='F-ChuckDeviceController-Plugin-PluginPermissions-AddControllers'></a>
### AddControllers `constants`

##### Summary

Add new ASP.NET Mvc controller routes

<a name='F-ChuckDeviceController-Plugin-PluginPermissions-AddInstances'></a>
### AddInstances `constants`

##### Summary

Add new instances

<a name='F-ChuckDeviceController-Plugin-PluginPermissions-AddJobControllers'></a>
### AddJobControllers `constants`

##### Summary

Add new job controller instances for devices

<a name='F-ChuckDeviceController-Plugin-PluginPermissions-All'></a>
### All `constants`

##### Summary

All available permissions

<a name='F-ChuckDeviceController-Plugin-PluginPermissions-DeleteDatabase'></a>
### DeleteDatabase `constants`

##### Summary

Delete database entities (NOTE: Should probably remove since Delete == Write essentially but would be nice to separate it)

<a name='F-ChuckDeviceController-Plugin-PluginPermissions-None'></a>
### None `constants`

##### Summary

No extra permissions

<a name='F-ChuckDeviceController-Plugin-PluginPermissions-ReadDatabase'></a>
### ReadDatabase `constants`

##### Summary

Read database entities

<a name='F-ChuckDeviceController-Plugin-PluginPermissions-WriteDatabase'></a>
### WriteDatabase `constants`

##### Summary

Write database entities

<a name='T-ChuckDeviceController-Plugin-PluginPermissionsAttribute'></a>
## PluginPermissionsAttribute `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Defines which permissions the plugin is going to request
in order to operate correctly.

<a name='M-ChuckDeviceController-Plugin-PluginPermissionsAttribute-#ctor-ChuckDeviceController-Plugin-PluginPermissions-'></a>
### #ctor(permissions) `constructor`

##### Summary

Instantiates a new plugin permissions attribute.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| permissions | [ChuckDeviceController.Plugin.PluginPermissions](#T-ChuckDeviceController-Plugin-PluginPermissions 'ChuckDeviceController.Plugin.PluginPermissions') | Plugin permissions to request upon load. |

<a name='P-ChuckDeviceController-Plugin-PluginPermissionsAttribute-Permissions'></a>
### Permissions `property`

##### Summary

Gets the requested permissions of the plugin.

<a name='T-ChuckDeviceController-Plugin-Services-PluginServiceAttribute'></a>
## PluginServiceAttribute `type`

##### Namespace

ChuckDeviceController.Plugin.Services

##### Summary

Registers plugin service classes that are marked with the
'PluginService' attribute with the host application in 
order to be used with dependency injection.

<a name='M-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-#ctor'></a>
### #ctor() `constructor`

##### Summary



##### Parameters

This constructor has no parameters.

<a name='M-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-#ctor-System-Type,System-Type,ChuckDeviceController-Plugin-Services-PluginServiceProvider,Microsoft-Extensions-DependencyInjection-ServiceLifetime-'></a>
### #ctor(serviceType,proxyType,provider,lifetime) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| serviceType | [System.Type](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Type 'System.Type') |  |
| proxyType | [System.Type](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Type 'System.Type') |  |
| provider | [ChuckDeviceController.Plugin.Services.PluginServiceProvider](#T-ChuckDeviceController-Plugin-Services-PluginServiceProvider 'ChuckDeviceController.Plugin.Services.PluginServiceProvider') |  |
| lifetime | [Microsoft.Extensions.DependencyInjection.ServiceLifetime](#T-Microsoft-Extensions-DependencyInjection-ServiceLifetime 'Microsoft.Extensions.DependencyInjection.ServiceLifetime') |  |

<a name='P-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-Lifetime'></a>
### Lifetime `property`

##### Summary

Gets or sets the service lifetime for the plugin service.

<a name='P-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-Provider'></a>
### Provider `property`

##### Summary

Gets or sets who provided the service.

<a name='P-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-ProxyType'></a>
### ProxyType `property`

##### Summary

Gets or sets the service implementation type.

<a name='P-ChuckDeviceController-Plugin-Services-PluginServiceAttribute-ServiceType'></a>
### ServiceType `property`

##### Summary

Gets or sets the service contract type.

<a name='T-ChuckDeviceController-Plugin-Services-PluginServiceProvider'></a>
## PluginServiceProvider `type`

##### Namespace

ChuckDeviceController.Plugin.Services

##### Summary

Determines who provided the plugin service to register with
dependency injection.

<a name='F-ChuckDeviceController-Plugin-Services-PluginServiceProvider-Host'></a>
### Host `constants`

##### Summary

Service was provided by the host application.

<a name='F-ChuckDeviceController-Plugin-Services-PluginServiceProvider-Plugin'></a>
### Plugin `constants`

##### Summary

Service was provided by the plugin.

<a name='T-ChuckDeviceController-Plugin-StaticFilesLocation'></a>
## StaticFilesLocation `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Determines the location of any static files and folders
i.e. 'wwwroot'

<a name='F-ChuckDeviceController-Plugin-StaticFilesLocation-External'></a>
### External `constants`

##### Summary

Static files are located externally

<a name='F-ChuckDeviceController-Plugin-StaticFilesLocation-None'></a>
### None `constants`

##### Summary

No static files from plugin

<a name='F-ChuckDeviceController-Plugin-StaticFilesLocation-Resources'></a>
### Resources `constants`

##### Summary

Static files are embedded in a resource file

<a name='T-ChuckDeviceController-Plugin-StaticFilesLocationAttribute'></a>
## StaticFilesLocationAttribute `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Determines where the static files (i.e. 'wwwroot') will be located to the plugin.

<a name='M-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-#ctor-ChuckDeviceController-Plugin-StaticFilesLocation-'></a>
### #ctor(location) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| location | [ChuckDeviceController.Plugin.StaticFilesLocation](#T-ChuckDeviceController-Plugin-StaticFilesLocation 'ChuckDeviceController.Plugin.StaticFilesLocation') |  |

<a name='P-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-Location'></a>
### Location `property`

##### Summary

Gets the location of the static files.
