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
- [IConfigurationHost](#T-ChuckDeviceController-Plugin-IConfigurationHost 'ChuckDeviceController.Plugin.IConfigurationHost')
    - [GetConfiguration(jsonFileName,sectionName)](#M-ChuckDeviceController-Plugin-IConfigurationHost-GetConfiguration-System-String,System-String- 'ChuckDeviceController.Plugin.IConfigurationHost.GetConfiguration(System.String,System.String)')
    - [GetValue\`\`1(name,defaultValue,sectionName)](#M-ChuckDeviceController-Plugin-IConfigurationHost-GetValue``1-System-String,``0,System-String- 'ChuckDeviceController.Plugin.IConfigurationHost.GetValue``1(System.String,``0,System.String)')
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
- [IFileStorageHost](#T-ChuckDeviceController-Plugin-IFileStorageHost 'ChuckDeviceController.Plugin.IFileStorageHost')
- [IJobControllerServiceEvents](#T-ChuckDeviceController-Plugin-IJobControllerServiceEvents 'ChuckDeviceController.Plugin.IJobControllerServiceEvents')
- [IJobControllerServiceHost](#T-ChuckDeviceController-Plugin-IJobControllerServiceHost 'ChuckDeviceController.Plugin.IJobControllerServiceHost')
    - [AddJobControllerAsync(name,controller)](#M-ChuckDeviceController-Plugin-IJobControllerServiceHost-AddJobControllerAsync-System-String,ChuckDeviceController-Common-Jobs-IJobController- 'ChuckDeviceController.Plugin.IJobControllerServiceHost.AddJobControllerAsync(System.String,ChuckDeviceController.Common.Jobs.IJobController)')
    - [AssignDeviceToJobControllerAsync(device,jobControllerName)](#M-ChuckDeviceController-Plugin-IJobControllerServiceHost-AssignDeviceToJobControllerAsync-ChuckDeviceController-Common-Data-Contracts-IDevice,System-String- 'ChuckDeviceController.Plugin.IJobControllerServiceHost.AssignDeviceToJobControllerAsync(ChuckDeviceController.Common.Data.Contracts.IDevice,System.String)')
- [ILoadData](#T-ChuckDeviceController-Plugin-ILoadData 'ChuckDeviceController.Plugin.ILoadData')
    - [Load\`\`1(folderName,fileName)](#M-ChuckDeviceController-Plugin-ILoadData-Load``1-System-String,System-String- 'ChuckDeviceController.Plugin.ILoadData.Load``1(System.String,System.String)')
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
- [IPlugin](#T-ChuckDeviceController-Plugin-IPlugin 'ChuckDeviceController.Plugin.IPlugin')
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
- [IRepository\`2](#T-ChuckDeviceController-Plugin-IRepository`2 'ChuckDeviceController.Plugin.IRepository`2')
    - [GetByIdAsync(id)](#M-ChuckDeviceController-Plugin-IRepository`2-GetByIdAsync-`1- 'ChuckDeviceController.Plugin.IRepository`2.GetByIdAsync(`1)')
    - [GetListAsync()](#M-ChuckDeviceController-Plugin-IRepository`2-GetListAsync 'ChuckDeviceController.Plugin.IRepository`2.GetListAsync')
- [ISaveData](#T-ChuckDeviceController-Plugin-ISaveData 'ChuckDeviceController.Plugin.ISaveData')
    - [Save\`\`1(data,folderName,name,prettyPrint)](#M-ChuckDeviceController-Plugin-ISaveData-Save``1-``0,System-String,System-String,System-Boolean- 'ChuckDeviceController.Plugin.ISaveData.Save``1(``0,System.String,System.String,System.Boolean)')
- [ISettingsProperty](#T-ChuckDeviceController-Plugin-ISettingsProperty 'ChuckDeviceController.Plugin.ISettingsProperty')
    - [Class](#P-ChuckDeviceController-Plugin-ISettingsProperty-Class 'ChuckDeviceController.Plugin.ISettingsProperty.Class')
    - [DefaultValue](#P-ChuckDeviceController-Plugin-ISettingsProperty-DefaultValue 'ChuckDeviceController.Plugin.ISettingsProperty.DefaultValue')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-ISettingsProperty-DisplayIndex 'ChuckDeviceController.Plugin.ISettingsProperty.DisplayIndex')
    - [Group](#P-ChuckDeviceController-Plugin-ISettingsProperty-Group 'ChuckDeviceController.Plugin.ISettingsProperty.Group')
    - [IsRequired](#P-ChuckDeviceController-Plugin-ISettingsProperty-IsRequired 'ChuckDeviceController.Plugin.ISettingsProperty.IsRequired')
    - [Name](#P-ChuckDeviceController-Plugin-ISettingsProperty-Name 'ChuckDeviceController.Plugin.ISettingsProperty.Name')
    - [Style](#P-ChuckDeviceController-Plugin-ISettingsProperty-Style 'ChuckDeviceController.Plugin.ISettingsProperty.Style')
    - [Text](#P-ChuckDeviceController-Plugin-ISettingsProperty-Text 'ChuckDeviceController.Plugin.ISettingsProperty.Text')
    - [Type](#P-ChuckDeviceController-Plugin-ISettingsProperty-Type 'ChuckDeviceController.Plugin.ISettingsProperty.Type')
    - [Validate](#P-ChuckDeviceController-Plugin-ISettingsProperty-Validate 'ChuckDeviceController.Plugin.ISettingsProperty.Validate')
    - [Value](#P-ChuckDeviceController-Plugin-ISettingsProperty-Value 'ChuckDeviceController.Plugin.ISettingsProperty.Value')
- [ISettingsPropertyEvents](#T-ChuckDeviceController-Plugin-ISettingsPropertyEvents 'ChuckDeviceController.Plugin.ISettingsPropertyEvents')
    - [OnClick()](#M-ChuckDeviceController-Plugin-ISettingsPropertyEvents-OnClick-ChuckDeviceController-Plugin-ISettingsProperty- 'ChuckDeviceController.Plugin.ISettingsPropertyEvents.OnClick(ChuckDeviceController.Plugin.ISettingsProperty)')
    - [OnSave()](#M-ChuckDeviceController-Plugin-ISettingsPropertyEvents-OnSave-ChuckDeviceController-Plugin-ISettingsProperty- 'ChuckDeviceController.Plugin.ISettingsPropertyEvents.OnSave(ChuckDeviceController.Plugin.ISettingsProperty)')
    - [OnToggle()](#M-ChuckDeviceController-Plugin-ISettingsPropertyEvents-OnToggle-ChuckDeviceController-Plugin-ISettingsProperty- 'ChuckDeviceController.Plugin.ISettingsPropertyEvents.OnToggle(ChuckDeviceController.Plugin.ISettingsProperty)')
- [ISettingsPropertyGroup](#T-ChuckDeviceController-Plugin-ISettingsPropertyGroup 'ChuckDeviceController.Plugin.ISettingsPropertyGroup')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-ISettingsPropertyGroup-DisplayIndex 'ChuckDeviceController.Plugin.ISettingsPropertyGroup.DisplayIndex')
    - [Id](#P-ChuckDeviceController-Plugin-ISettingsPropertyGroup-Id 'ChuckDeviceController.Plugin.ISettingsPropertyGroup.Id')
    - [Text](#P-ChuckDeviceController-Plugin-ISettingsPropertyGroup-Text 'ChuckDeviceController.Plugin.ISettingsPropertyGroup.Text')
- [ISettingsTab](#T-ChuckDeviceController-Plugin-ISettingsTab 'ChuckDeviceController.Plugin.ISettingsTab')
    - [Anchor](#P-ChuckDeviceController-Plugin-ISettingsTab-Anchor 'ChuckDeviceController.Plugin.ISettingsTab.Anchor')
    - [Class](#P-ChuckDeviceController-Plugin-ISettingsTab-Class 'ChuckDeviceController.Plugin.ISettingsTab.Class')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-ISettingsTab-DisplayIndex 'ChuckDeviceController.Plugin.ISettingsTab.DisplayIndex')
    - [Id](#P-ChuckDeviceController-Plugin-ISettingsTab-Id 'ChuckDeviceController.Plugin.ISettingsTab.Id')
    - [Style](#P-ChuckDeviceController-Plugin-ISettingsTab-Style 'ChuckDeviceController.Plugin.ISettingsTab.Style')
    - [Text](#P-ChuckDeviceController-Plugin-ISettingsTab-Text 'ChuckDeviceController.Plugin.ISettingsTab.Text')
- [ISidebarItem](#T-ChuckDeviceController-Plugin-ISidebarItem 'ChuckDeviceController.Plugin.ISidebarItem')
    - [ActionName](#P-ChuckDeviceController-Plugin-ISidebarItem-ActionName 'ChuckDeviceController.Plugin.ISidebarItem.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugin-ISidebarItem-ControllerName 'ChuckDeviceController.Plugin.ISidebarItem.ControllerName')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-ISidebarItem-DisplayIndex 'ChuckDeviceController.Plugin.ISidebarItem.DisplayIndex')
    - [Icon](#P-ChuckDeviceController-Plugin-ISidebarItem-Icon 'ChuckDeviceController.Plugin.ISidebarItem.Icon')
    - [IsDisabled](#P-ChuckDeviceController-Plugin-ISidebarItem-IsDisabled 'ChuckDeviceController.Plugin.ISidebarItem.IsDisabled')
    - [IsSeparator](#P-ChuckDeviceController-Plugin-ISidebarItem-IsSeparator 'ChuckDeviceController.Plugin.ISidebarItem.IsSeparator')
    - [Text](#P-ChuckDeviceController-Plugin-ISidebarItem-Text 'ChuckDeviceController.Plugin.ISidebarItem.Text')
- [IUiEvents](#T-ChuckDeviceController-Plugin-IUiEvents 'ChuckDeviceController.Plugin.IUiEvents')
- [IUiHost](#T-ChuckDeviceController-Plugin-IUiHost 'ChuckDeviceController.Plugin.IUiHost')
    - [DashboardStatsItems](#P-ChuckDeviceController-Plugin-IUiHost-DashboardStatsItems 'ChuckDeviceController.Plugin.IUiHost.DashboardStatsItems')
    - [DashboardTiles](#P-ChuckDeviceController-Plugin-IUiHost-DashboardTiles 'ChuckDeviceController.Plugin.IUiHost.DashboardTiles')
    - [SettingsProperties](#P-ChuckDeviceController-Plugin-IUiHost-SettingsProperties 'ChuckDeviceController.Plugin.IUiHost.SettingsProperties')
    - [SettingsTabs](#P-ChuckDeviceController-Plugin-IUiHost-SettingsTabs 'ChuckDeviceController.Plugin.IUiHost.SettingsTabs')
    - [SidebarItems](#P-ChuckDeviceController-Plugin-IUiHost-SidebarItems 'ChuckDeviceController.Plugin.IUiHost.SidebarItems')
    - [AddDashboardStatisticAsync(stat)](#M-ChuckDeviceController-Plugin-IUiHost-AddDashboardStatisticAsync-ChuckDeviceController-Plugin-IDashboardStatsItem- 'ChuckDeviceController.Plugin.IUiHost.AddDashboardStatisticAsync(ChuckDeviceController.Plugin.IDashboardStatsItem)')
    - [AddDashboardStatisticsAsync(stats)](#M-ChuckDeviceController-Plugin-IUiHost-AddDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardStatsItem}- 'ChuckDeviceController.Plugin.IUiHost.AddDashboardStatisticsAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardStatsItem})')
    - [AddDashboardTileAsync(tile)](#M-ChuckDeviceController-Plugin-IUiHost-AddDashboardTileAsync-ChuckDeviceController-Plugin-IDashboardTile- 'ChuckDeviceController.Plugin.IUiHost.AddDashboardTileAsync(ChuckDeviceController.Plugin.IDashboardTile)')
    - [AddDashboardTilesAsync(tiles)](#M-ChuckDeviceController-Plugin-IUiHost-AddDashboardTilesAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardTile}- 'ChuckDeviceController.Plugin.IUiHost.AddDashboardTilesAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardTile})')
    - [AddSettingsPropertiesAsync(tabId,properties)](#M-ChuckDeviceController-Plugin-IUiHost-AddSettingsPropertiesAsync-System-String,System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-SettingsProperty}- 'ChuckDeviceController.Plugin.IUiHost.AddSettingsPropertiesAsync(System.String,System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SettingsProperty})')
    - [AddSettingsPropertyAsync(tabId,property)](#M-ChuckDeviceController-Plugin-IUiHost-AddSettingsPropertyAsync-System-String,ChuckDeviceController-Plugin-SettingsProperty- 'ChuckDeviceController.Plugin.IUiHost.AddSettingsPropertyAsync(System.String,ChuckDeviceController.Plugin.SettingsProperty)')
    - [AddSettingsTabAsync(tab)](#M-ChuckDeviceController-Plugin-IUiHost-AddSettingsTabAsync-ChuckDeviceController-Plugin-SettingsTab- 'ChuckDeviceController.Plugin.IUiHost.AddSettingsTabAsync(ChuckDeviceController.Plugin.SettingsTab)')
    - [AddSidebarItemAsync(header)](#M-ChuckDeviceController-Plugin-IUiHost-AddSidebarItemAsync-ChuckDeviceController-Plugin-SidebarItem- 'ChuckDeviceController.Plugin.IUiHost.AddSidebarItemAsync(ChuckDeviceController.Plugin.SidebarItem)')
    - [AddSidebarItemsAsync(headers)](#M-ChuckDeviceController-Plugin-IUiHost-AddSidebarItemsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-SidebarItem}- 'ChuckDeviceController.Plugin.IUiHost.AddSidebarItemsAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SidebarItem})')
    - [GetSettingsPropertyValue\`\`1(name)](#M-ChuckDeviceController-Plugin-IUiHost-GetSettingsPropertyValue``1-System-String- 'ChuckDeviceController.Plugin.IUiHost.GetSettingsPropertyValue``1(System.String)')
    - [UpdateDashboardStatisticAsync(stat)](#M-ChuckDeviceController-Plugin-IUiHost-UpdateDashboardStatisticAsync-ChuckDeviceController-Plugin-IDashboardStatsItem- 'ChuckDeviceController.Plugin.IUiHost.UpdateDashboardStatisticAsync(ChuckDeviceController.Plugin.IDashboardStatsItem)')
    - [UpdateDashboardStatisticsAsync(stats)](#M-ChuckDeviceController-Plugin-IUiHost-UpdateDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-IDashboardStatsItem}- 'ChuckDeviceController.Plugin.IUiHost.UpdateDashboardStatisticsAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.IDashboardStatsItem})')
- [IWebPlugin](#T-ChuckDeviceController-Plugin-IWebPlugin 'ChuckDeviceController.Plugin.IWebPlugin')
    - [Configure(appBuilder)](#M-ChuckDeviceController-Plugin-IWebPlugin-Configure-Microsoft-AspNetCore-Builder-WebApplication- 'ChuckDeviceController.Plugin.IWebPlugin.Configure(Microsoft.AspNetCore.Builder.WebApplication)')
    - [ConfigureMvcBuilder(mvcBuilder)](#M-ChuckDeviceController-Plugin-IWebPlugin-ConfigureMvcBuilder-Microsoft-Extensions-DependencyInjection-IMvcBuilder- 'ChuckDeviceController.Plugin.IWebPlugin.ConfigureMvcBuilder(Microsoft.Extensions.DependencyInjection.IMvcBuilder)')
    - [ConfigureServices(services)](#M-ChuckDeviceController-Plugin-IWebPlugin-ConfigureServices-Microsoft-Extensions-DependencyInjection-IServiceCollection- 'ChuckDeviceController.Plugin.IWebPlugin.ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)')
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
- [RequestTimings](#T-ChuckDeviceController-Plugin-RequestTimings 'ChuckDeviceController.Plugin.RequestTimings')
    - [#ctor()](#M-ChuckDeviceController-Plugin-RequestTimings-#ctor 'ChuckDeviceController.Plugin.RequestTimings.#ctor')
    - [Average](#P-ChuckDeviceController-Plugin-RequestTimings-Average 'ChuckDeviceController.Plugin.RequestTimings.Average')
    - [DecimalPlaces](#P-ChuckDeviceController-Plugin-RequestTimings-DecimalPlaces 'ChuckDeviceController.Plugin.RequestTimings.DecimalPlaces')
    - [Fastest](#P-ChuckDeviceController-Plugin-RequestTimings-Fastest 'ChuckDeviceController.Plugin.RequestTimings.Fastest')
    - [IsCloned](#P-ChuckDeviceController-Plugin-RequestTimings-IsCloned 'ChuckDeviceController.Plugin.RequestTimings.IsCloned')
    - [LogTimings](#P-ChuckDeviceController-Plugin-RequestTimings-LogTimings 'ChuckDeviceController.Plugin.RequestTimings.LogTimings')
    - [Requests](#P-ChuckDeviceController-Plugin-RequestTimings-Requests 'ChuckDeviceController.Plugin.RequestTimings.Requests')
    - [Slowest](#P-ChuckDeviceController-Plugin-RequestTimings-Slowest 'ChuckDeviceController.Plugin.RequestTimings.Slowest')
    - [Total](#P-ChuckDeviceController-Plugin-RequestTimings-Total 'ChuckDeviceController.Plugin.RequestTimings.Total')
    - [TrimmedAverage](#P-ChuckDeviceController-Plugin-RequestTimings-TrimmedAverage 'ChuckDeviceController.Plugin.RequestTimings.TrimmedAverage')
    - [Clone()](#M-ChuckDeviceController-Plugin-RequestTimings-Clone 'ChuckDeviceController.Plugin.RequestTimings.Clone')
    - [Increment(stopWatch)](#M-ChuckDeviceController-Plugin-RequestTimings-Increment-System-Diagnostics-Stopwatch- 'ChuckDeviceController.Plugin.RequestTimings.Increment(System.Diagnostics.Stopwatch)')
    - [Increment(totalTicks)](#M-ChuckDeviceController-Plugin-RequestTimings-Increment-System-Int64- 'ChuckDeviceController.Plugin.RequestTimings.Increment(System.Int64)')
- [SettingsProperty](#T-ChuckDeviceController-Plugin-SettingsProperty 'ChuckDeviceController.Plugin.SettingsProperty')
    - [#ctor()](#M-ChuckDeviceController-Plugin-SettingsProperty-#ctor 'ChuckDeviceController.Plugin.SettingsProperty.#ctor')
    - [#ctor(text,name,type,value,defaultValue,displayIndex,isRequired,validate,className,style,group)](#M-ChuckDeviceController-Plugin-SettingsProperty-#ctor-System-String,System-String,ChuckDeviceController-Plugin-SettingsPropertyType,System-Object,System-Object,System-UInt32,System-Boolean,System-Boolean,System-String,System-String,ChuckDeviceController-Plugin-SettingsPropertyGroup- 'ChuckDeviceController.Plugin.SettingsProperty.#ctor(System.String,System.String,ChuckDeviceController.Plugin.SettingsPropertyType,System.Object,System.Object,System.UInt32,System.Boolean,System.Boolean,System.String,System.String,ChuckDeviceController.Plugin.SettingsPropertyGroup)')
    - [Class](#P-ChuckDeviceController-Plugin-SettingsProperty-Class 'ChuckDeviceController.Plugin.SettingsProperty.Class')
    - [DefaultValue](#P-ChuckDeviceController-Plugin-SettingsProperty-DefaultValue 'ChuckDeviceController.Plugin.SettingsProperty.DefaultValue')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-SettingsProperty-DisplayIndex 'ChuckDeviceController.Plugin.SettingsProperty.DisplayIndex')
    - [Group](#P-ChuckDeviceController-Plugin-SettingsProperty-Group 'ChuckDeviceController.Plugin.SettingsProperty.Group')
    - [IsRequired](#P-ChuckDeviceController-Plugin-SettingsProperty-IsRequired 'ChuckDeviceController.Plugin.SettingsProperty.IsRequired')
    - [Name](#P-ChuckDeviceController-Plugin-SettingsProperty-Name 'ChuckDeviceController.Plugin.SettingsProperty.Name')
    - [Style](#P-ChuckDeviceController-Plugin-SettingsProperty-Style 'ChuckDeviceController.Plugin.SettingsProperty.Style')
    - [Text](#P-ChuckDeviceController-Plugin-SettingsProperty-Text 'ChuckDeviceController.Plugin.SettingsProperty.Text')
    - [Type](#P-ChuckDeviceController-Plugin-SettingsProperty-Type 'ChuckDeviceController.Plugin.SettingsProperty.Type')
    - [Validate](#P-ChuckDeviceController-Plugin-SettingsProperty-Validate 'ChuckDeviceController.Plugin.SettingsProperty.Validate')
    - [Value](#P-ChuckDeviceController-Plugin-SettingsProperty-Value 'ChuckDeviceController.Plugin.SettingsProperty.Value')
- [SettingsPropertyGroup](#T-ChuckDeviceController-Plugin-SettingsPropertyGroup 'ChuckDeviceController.Plugin.SettingsPropertyGroup')
    - [#ctor()](#M-ChuckDeviceController-Plugin-SettingsPropertyGroup-#ctor 'ChuckDeviceController.Plugin.SettingsPropertyGroup.#ctor')
    - [#ctor(id,text,displayIndex)](#M-ChuckDeviceController-Plugin-SettingsPropertyGroup-#ctor-System-String,System-String,System-UInt32- 'ChuckDeviceController.Plugin.SettingsPropertyGroup.#ctor(System.String,System.String,System.UInt32)')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-SettingsPropertyGroup-DisplayIndex 'ChuckDeviceController.Plugin.SettingsPropertyGroup.DisplayIndex')
    - [Id](#P-ChuckDeviceController-Plugin-SettingsPropertyGroup-Id 'ChuckDeviceController.Plugin.SettingsPropertyGroup.Id')
    - [Text](#P-ChuckDeviceController-Plugin-SettingsPropertyGroup-Text 'ChuckDeviceController.Plugin.SettingsPropertyGroup.Text')
    - [Equals(obj)](#M-ChuckDeviceController-Plugin-SettingsPropertyGroup-Equals-System-Object- 'ChuckDeviceController.Plugin.SettingsPropertyGroup.Equals(System.Object)')
    - [Equals(other)](#M-ChuckDeviceController-Plugin-SettingsPropertyGroup-Equals-ChuckDeviceController-Plugin-SettingsPropertyGroup- 'ChuckDeviceController.Plugin.SettingsPropertyGroup.Equals(ChuckDeviceController.Plugin.SettingsPropertyGroup)')
    - [GetHashCode()](#M-ChuckDeviceController-Plugin-SettingsPropertyGroup-GetHashCode 'ChuckDeviceController.Plugin.SettingsPropertyGroup.GetHashCode')
- [SettingsPropertyType](#T-ChuckDeviceController-Plugin-SettingsPropertyType 'ChuckDeviceController.Plugin.SettingsPropertyType')
    - [CheckBox](#F-ChuckDeviceController-Plugin-SettingsPropertyType-CheckBox 'ChuckDeviceController.Plugin.SettingsPropertyType.CheckBox')
    - [Number](#F-ChuckDeviceController-Plugin-SettingsPropertyType-Number 'ChuckDeviceController.Plugin.SettingsPropertyType.Number')
    - [Select](#F-ChuckDeviceController-Plugin-SettingsPropertyType-Select 'ChuckDeviceController.Plugin.SettingsPropertyType.Select')
    - [Text](#F-ChuckDeviceController-Plugin-SettingsPropertyType-Text 'ChuckDeviceController.Plugin.SettingsPropertyType.Text')
    - [TextArea](#F-ChuckDeviceController-Plugin-SettingsPropertyType-TextArea 'ChuckDeviceController.Plugin.SettingsPropertyType.TextArea')
- [SettingsTab](#T-ChuckDeviceController-Plugin-SettingsTab 'ChuckDeviceController.Plugin.SettingsTab')
    - [#ctor()](#M-ChuckDeviceController-Plugin-SettingsTab-#ctor 'ChuckDeviceController.Plugin.SettingsTab.#ctor')
    - [#ctor(id,text,anchor,displayIndex,className,style)](#M-ChuckDeviceController-Plugin-SettingsTab-#ctor-System-String,System-String,System-String,System-UInt32,System-String,System-String- 'ChuckDeviceController.Plugin.SettingsTab.#ctor(System.String,System.String,System.String,System.UInt32,System.String,System.String)')
    - [Anchor](#P-ChuckDeviceController-Plugin-SettingsTab-Anchor 'ChuckDeviceController.Plugin.SettingsTab.Anchor')
    - [Class](#P-ChuckDeviceController-Plugin-SettingsTab-Class 'ChuckDeviceController.Plugin.SettingsTab.Class')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-SettingsTab-DisplayIndex 'ChuckDeviceController.Plugin.SettingsTab.DisplayIndex')
    - [Id](#P-ChuckDeviceController-Plugin-SettingsTab-Id 'ChuckDeviceController.Plugin.SettingsTab.Id')
    - [Style](#P-ChuckDeviceController-Plugin-SettingsTab-Style 'ChuckDeviceController.Plugin.SettingsTab.Style')
    - [Text](#P-ChuckDeviceController-Plugin-SettingsTab-Text 'ChuckDeviceController.Plugin.SettingsTab.Text')
- [SidebarItem](#T-ChuckDeviceController-Plugin-SidebarItem 'ChuckDeviceController.Plugin.SidebarItem')
    - [#ctor()](#M-ChuckDeviceController-Plugin-SidebarItem-#ctor 'ChuckDeviceController.Plugin.SidebarItem.#ctor')
    - [#ctor(text,controllerName,actionName,icon,displayIndex,isDropdown,dropdownItems,isDisabled,isSeparator)](#M-ChuckDeviceController-Plugin-SidebarItem-#ctor-System-String,System-String,System-String,System-String,System-UInt32,System-Boolean,System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-SidebarItem},System-Boolean,System-Boolean- 'ChuckDeviceController.Plugin.SidebarItem.#ctor(System.String,System.String,System.String,System.String,System.UInt32,System.Boolean,System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SidebarItem},System.Boolean,System.Boolean)')
    - [ActionName](#P-ChuckDeviceController-Plugin-SidebarItem-ActionName 'ChuckDeviceController.Plugin.SidebarItem.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugin-SidebarItem-ControllerName 'ChuckDeviceController.Plugin.SidebarItem.ControllerName')
    - [DisplayIndex](#P-ChuckDeviceController-Plugin-SidebarItem-DisplayIndex 'ChuckDeviceController.Plugin.SidebarItem.DisplayIndex')
    - [DropdownItems](#P-ChuckDeviceController-Plugin-SidebarItem-DropdownItems 'ChuckDeviceController.Plugin.SidebarItem.DropdownItems')
    - [Icon](#P-ChuckDeviceController-Plugin-SidebarItem-Icon 'ChuckDeviceController.Plugin.SidebarItem.Icon')
    - [IsDisabled](#P-ChuckDeviceController-Plugin-SidebarItem-IsDisabled 'ChuckDeviceController.Plugin.SidebarItem.IsDisabled')
    - [IsDropdown](#P-ChuckDeviceController-Plugin-SidebarItem-IsDropdown 'ChuckDeviceController.Plugin.SidebarItem.IsDropdown')
    - [IsSeparator](#P-ChuckDeviceController-Plugin-SidebarItem-IsSeparator 'ChuckDeviceController.Plugin.SidebarItem.IsSeparator')
    - [Text](#P-ChuckDeviceController-Plugin-SidebarItem-Text 'ChuckDeviceController.Plugin.SidebarItem.Text')
- [StaticFilesLocation](#T-ChuckDeviceController-Plugin-StaticFilesLocation 'ChuckDeviceController.Plugin.StaticFilesLocation')
    - [External](#F-ChuckDeviceController-Plugin-StaticFilesLocation-External 'ChuckDeviceController.Plugin.StaticFilesLocation.External')
    - [None](#F-ChuckDeviceController-Plugin-StaticFilesLocation-None 'ChuckDeviceController.Plugin.StaticFilesLocation.None')
    - [Resources](#F-ChuckDeviceController-Plugin-StaticFilesLocation-Resources 'ChuckDeviceController.Plugin.StaticFilesLocation.Resources')
- [StaticFilesLocationAttribute](#T-ChuckDeviceController-Plugin-StaticFilesLocationAttribute 'ChuckDeviceController.Plugin.StaticFilesLocationAttribute')
    - [#ctor(views,webRoot)](#M-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-#ctor-ChuckDeviceController-Plugin-StaticFilesLocation,ChuckDeviceController-Plugin-StaticFilesLocation- 'ChuckDeviceController.Plugin.StaticFilesLocationAttribute.#ctor(ChuckDeviceController.Plugin.StaticFilesLocation,ChuckDeviceController.Plugin.StaticFilesLocation)')
    - [Views](#P-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-Views 'ChuckDeviceController.Plugin.StaticFilesLocationAttribute.Views')
    - [WebRoot](#P-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-WebRoot 'ChuckDeviceController.Plugin.StaticFilesLocationAttribute.WebRoot')

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

<a name='T-ChuckDeviceController-Plugin-IConfigurationHost'></a>
## IConfigurationHost `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

This interface contract can be used by all plugin modules to load setting and configuration data.

    The default implementation which is loaded if no other plugin registers an instance uses 
    appsettings.json to store configuration data to be used by Plugins.

    An instance of this interface is available via the DI container, any custom implementations
    must be configured to be used in the DI contaner when being initialised.

##### Remarks

This class can be customised by the host application, if no implementation is provided then
    a default implementation is provided.

<a name='M-ChuckDeviceController-Plugin-IConfigurationHost-GetConfiguration-System-String,System-String-'></a>
### GetConfiguration(jsonFileName,sectionName) `method`

##### Summary

Retrieves a configuration instance.

##### Returns

Configuration file instance initialised with the required settings.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| jsonFileName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Name of JSON file name to be used. If a JSON cofiguration file is not provided, the default
    'appsettings.json' will be loaded from the calling plugin's root folder. |
| sectionName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Name of configuration data section required. |

<a name='M-ChuckDeviceController-Plugin-IConfigurationHost-GetValue``1-System-String,``0,System-String-'></a>
### GetValue\`\`1(name,defaultValue,sectionName) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| defaultValue | [\`\`0](#T-``0 '``0') |  |
| sectionName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Class who's settings are being requested. |

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

<a name='T-ChuckDeviceController-Plugin-IFileStorageHost'></a>
## IFileStorageHost `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Interface contract used for reading data from as well as
    persisting data to storage. The type of storage used will
    depend on the implementation.

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

<a name='T-ChuckDeviceController-Plugin-ILoadData'></a>
## ILoadData `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary



<a name='M-ChuckDeviceController-Plugin-ILoadData-Load``1-System-String,System-String-'></a>
### Load\`\`1(folderName,fileName) `method`

##### Summary

Loads file data of type T from the plugin's folder.

##### Returns

Type of data to be loaded or default type if exception occurs.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| folderName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Sub folder within plugin's folder, optional. If not set,
    searches root of plugin's folder. |
| fileName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | File name of storage file to load, including extension
    otherwise generic '.dat' extension will be appended. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Type of file data to be loaded. |

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

<a name='T-ChuckDeviceController-Plugin-IPlugin'></a>
## IPlugin `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Base Plugin interface contract all plugins are required to
inherit at a minimum.

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

<a name='T-ChuckDeviceController-Plugin-IRepository`2'></a>
## IRepository\`2 `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Repository contract for specific database entity types.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TEntity | Database entity contract type. |
| TId | Database entity primary key type. |

<a name='M-ChuckDeviceController-Plugin-IRepository`2-GetByIdAsync-`1-'></a>
### GetByIdAsync(id) `method`

##### Summary

Gets a database entity by primary key.

##### Returns

Returns a database entity.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [\`1](#T-`1 '`1') | Primary key of the database entity. |

<a name='M-ChuckDeviceController-Plugin-IRepository`2-GetListAsync'></a>
### GetListAsync() `method`

##### Summary

Gets a list of database entities.

##### Returns

Returns a list of database entities.

##### Parameters

This method has no parameters.

<a name='T-ChuckDeviceController-Plugin-ISaveData'></a>
## ISaveData `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary



<a name='M-ChuckDeviceController-Plugin-ISaveData-Save``1-``0,System-String,System-String,System-Boolean-'></a>
### Save\`\`1(data,folderName,name,prettyPrint) `method`

##### Summary

Saves file data of type T to the plugin's folder.

##### Returns

Returns

```
true
```

if successful, otherwise

```
false
```

.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| data | [\`\`0](#T-``0 '``0') | File data to be saved. |
| folderName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Sub folder within plugin's folder, optional. If not set,
    uses root of plugin's folder. |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | File name of storage file to save, including extension
    otherwise generic '.dat' extension will be appended. |
| prettyPrint | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Type of data to be saved. |

<a name='T-ChuckDeviceController-Plugin-ISettingsProperty'></a>
## ISettingsProperty `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Settings property interface contract used by plugins to
create UI setting elements in the dashboard.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-Class'></a>
### Class `property`

##### Summary

Gets or sets the CSS class name to use.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-DefaultValue'></a>
### DefaultValue `property`

##### Summary

Gets or sets the default value to use for the element, if
it supports it.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets a value used for sorting each HTML element
created for the properties.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-Group'></a>
### Group `property`

##### Summary

Gets or sets the group the settings property
will be in.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-IsRequired'></a>
### IsRequired `property`

##### Summary

Gets or sets a value determining whether the HTML element
value is required.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-Name'></a>
### Name `property`

##### Summary

Gets or sets the ID and name of the element.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-Style'></a>
### Style `property`

##### Summary

Gets or sets the raw CSS styling to use.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-Text'></a>
### Text `property`

##### Summary

Gets or sets the displayed text for the property, possibly
used in a label.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-Type'></a>
### Type `property`

##### Summary

Gets or sets the type of HTML element to create.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-Validate'></a>
### Validate `property`

##### Summary

Gets or sets a value determining whether to validate the
value of the HTML element.

<a name='P-ChuckDeviceController-Plugin-ISettingsProperty-Value'></a>
### Value `property`

##### Summary

Gets or sets the initial value to set for the element.

<a name='T-ChuckDeviceController-Plugin-ISettingsPropertyEvents'></a>
## ISettingsPropertyEvents `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary



<a name='M-ChuckDeviceController-Plugin-ISettingsPropertyEvents-OnClick-ChuckDeviceController-Plugin-ISettingsProperty-'></a>
### OnClick() `method`

##### Summary



##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugin-ISettingsPropertyEvents-OnSave-ChuckDeviceController-Plugin-ISettingsProperty-'></a>
### OnSave() `method`

##### Summary



##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugin-ISettingsPropertyEvents-OnToggle-ChuckDeviceController-Plugin-ISettingsProperty-'></a>
### OnToggle() `method`

##### Summary



##### Parameters

This method has no parameters.

<a name='T-ChuckDeviceController-Plugin-ISettingsPropertyGroup'></a>
## ISettingsPropertyGroup `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Interface contract for grouping settings properties.

<a name='P-ChuckDeviceController-Plugin-ISettingsPropertyGroup-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets a value used for sorting each HTML element
created for the properties.

<a name='P-ChuckDeviceController-Plugin-ISettingsPropertyGroup-Id'></a>
### Id `property`

##### Summary

Gets or sets the unique ID for the settings property group.

<a name='P-ChuckDeviceController-Plugin-ISettingsPropertyGroup-Text'></a>
### Text `property`

##### Summary

Gets or sets the display text for the settings property group.

<a name='T-ChuckDeviceController-Plugin-ISettingsTab'></a>
## ISettingsTab `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Settings tab interface contract to add UI settings for plugins.

<a name='P-ChuckDeviceController-Plugin-ISettingsTab-Anchor'></a>
### Anchor `property`

##### Summary

Gets or sets the html anchor tag name of the tab.
Note: No hash symbol needed.

<a name='P-ChuckDeviceController-Plugin-ISettingsTab-Class'></a>
### Class `property`

##### Summary

Gets or sets the CSS class name to use.

<a name='P-ChuckDeviceController-Plugin-ISettingsTab-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the display index of the tab in the tab list.

<a name='P-ChuckDeviceController-Plugin-ISettingsTab-Id'></a>
### Id `property`

##### Summary

Gets or sets the unique ID of the tab.

<a name='P-ChuckDeviceController-Plugin-ISettingsTab-Style'></a>
### Style `property`

##### Summary

Gets or sets the raw CSS styling to use.

<a name='P-ChuckDeviceController-Plugin-ISettingsTab-Text'></a>
### Text `property`

##### Summary

Gets or sets the display text of the tab.

<a name='T-ChuckDeviceController-Plugin-ISidebarItem'></a>
## ISidebarItem `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Side navigation bar plugin contract.

<a name='P-ChuckDeviceController-Plugin-ISidebarItem-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the sidebar item is clicked.

<a name='P-ChuckDeviceController-Plugin-ISidebarItem-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to.

<a name='P-ChuckDeviceController-Plugin-ISidebarItem-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the numeric display index order of
the sidebar item in the list of sidebar items.

<a name='P-ChuckDeviceController-Plugin-ISidebarItem-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the FontAwesome v6 icon key to use for 
the sidebar item. https://fontawesome.com/icons

<a name='P-ChuckDeviceController-Plugin-ISidebarItem-IsDisabled'></a>
### IsDisabled `property`

##### Summary

Gets or sets a value determining whether the
sidebar item is disabled or not.

<a name='P-ChuckDeviceController-Plugin-ISidebarItem-IsSeparator'></a>
### IsSeparator `property`

##### Summary

Gets or sets a value determining whether to insert a
separator instead of a dropdown item.

<a name='P-ChuckDeviceController-Plugin-ISidebarItem-Text'></a>
### Text `property`

##### Summary

Gets or sets the text to display for this sidebar
item.

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

<a name='P-ChuckDeviceController-Plugin-IUiHost-SettingsProperties'></a>
### SettingsProperties `property`

##### Summary

Gets a dictionary of settings properties for tabs registered by plugins.

<a name='P-ChuckDeviceController-Plugin-IUiHost-SettingsTabs'></a>
### SettingsTabs `property`

##### Summary

Gets a list of settings tabs registered by plugins.

<a name='P-ChuckDeviceController-Plugin-IUiHost-SidebarItems'></a>
### SidebarItems `property`

##### Summary

Gets a list of sidebar items registered by plugins.

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

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddSettingsPropertiesAsync-System-String,System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-SettingsProperty}-'></a>
### AddSettingsPropertiesAsync(tabId,properties) `method`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tabId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| properties | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SettingsProperty}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SettingsProperty}') |  |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddSettingsPropertyAsync-System-String,ChuckDeviceController-Plugin-SettingsProperty-'></a>
### AddSettingsPropertyAsync(tabId,property) `method`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tabId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| property | [ChuckDeviceController.Plugin.SettingsProperty](#T-ChuckDeviceController-Plugin-SettingsProperty 'ChuckDeviceController.Plugin.SettingsProperty') |  |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddSettingsTabAsync-ChuckDeviceController-Plugin-SettingsTab-'></a>
### AddSettingsTabAsync(tab) `method`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tab | [ChuckDeviceController.Plugin.SettingsTab](#T-ChuckDeviceController-Plugin-SettingsTab 'ChuckDeviceController.Plugin.SettingsTab') |  |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddSidebarItemAsync-ChuckDeviceController-Plugin-SidebarItem-'></a>
### AddSidebarItemAsync(header) `method`

##### Summary

Adds a item to the main
application's Mvc sidebar.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| header | [ChuckDeviceController.Plugin.SidebarItem](#T-ChuckDeviceController-Plugin-SidebarItem 'ChuckDeviceController.Plugin.SidebarItem') | Sidebar item to add. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-AddSidebarItemsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-SidebarItem}-'></a>
### AddSidebarItemsAsync(headers) `method`

##### Summary

Adds a list of items to the
main application's Mvc sidebar.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| headers | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SidebarItem}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SidebarItem}') | List of sidebar items to add. |

<a name='M-ChuckDeviceController-Plugin-IUiHost-GetSettingsPropertyValue``1-System-String-'></a>
### GetSettingsPropertyValue\`\`1(name) `method`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T |  |

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

Configures the application to set up middlewares, map routing rules, etc.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| appBuilder | [Microsoft.AspNetCore.Builder.WebApplication](#T-Microsoft-AspNetCore-Builder-WebApplication 'Microsoft.AspNetCore.Builder.WebApplication') | Provides the mechanisms to configure an application's request pipeline. |

<a name='M-ChuckDeviceController-Plugin-IWebPlugin-ConfigureMvcBuilder-Microsoft-Extensions-DependencyInjection-IMvcBuilder-'></a>
### ConfigureMvcBuilder(mvcBuilder) `method`

##### Summary

Provides an opportunity for plugins to configure Mvc Builder.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| mvcBuilder | [Microsoft.Extensions.DependencyInjection.IMvcBuilder](#T-Microsoft-Extensions-DependencyInjection-IMvcBuilder 'Microsoft.Extensions.DependencyInjection.IMvcBuilder') | IMvcBuilder instance that can be configured. |

<a name='M-ChuckDeviceController-Plugin-IWebPlugin-ConfigureServices-Microsoft-Extensions-DependencyInjection-IServiceCollection-'></a>
### ConfigureServices(services) `method`

##### Summary

Register services into the IServiceCollection to use with Dependency Injection.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| services | [Microsoft.Extensions.DependencyInjection.IServiceCollection](#T-Microsoft-Extensions-DependencyInjection-IServiceCollection 'Microsoft.Extensions.DependencyInjection.IServiceCollection') | Specifies the contract for a collection of service descriptors. |

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

<a name='T-ChuckDeviceController-Plugin-RequestTimings'></a>
## RequestTimings `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Used to contain timing data for requests.

This stores the number of requests and total time in milleseconds
serving the requests.

<a name='M-ChuckDeviceController-Plugin-RequestTimings-#ctor'></a>
### #ctor() `constructor`

##### Summary



##### Parameters

This constructor has no parameters.

<a name='P-ChuckDeviceController-Plugin-RequestTimings-Average'></a>
### Average `property`

##### Summary

Gets the average number of milliseconds per request.

<a name='P-ChuckDeviceController-Plugin-RequestTimings-DecimalPlaces'></a>
### DecimalPlaces `property`

##### Summary

Gets the number of decimal places the results should be rounded to, default is 5

<a name='P-ChuckDeviceController-Plugin-RequestTimings-Fastest'></a>
### Fastest `property`

##### Summary

Gets the total number of milliseconds used for the request that was quickest.

<a name='P-ChuckDeviceController-Plugin-RequestTimings-IsCloned'></a>
### IsCloned `property`

##### Summary

Gets a value determining whether the timings have been cloned or not.

<a name='P-ChuckDeviceController-Plugin-RequestTimings-LogTimings'></a>
### LogTimings `property`

##### Summary

Gets a value determining whether to log request times or not.

<a name='P-ChuckDeviceController-Plugin-RequestTimings-Requests'></a>
### Requests `property`

##### Summary

Gets the total number of requests made.

<a name='P-ChuckDeviceController-Plugin-RequestTimings-Slowest'></a>
### Slowest `property`

##### Summary

Gets the total number of milliseconds used for the request that was slowest.

<a name='P-ChuckDeviceController-Plugin-RequestTimings-Total'></a>
### Total `property`

##### Summary

Gets the total number of requests.

<a name='P-ChuckDeviceController-Plugin-RequestTimings-TrimmedAverage'></a>
### TrimmedAverage `property`

##### Summary

Gets the calculated trimmed average by removing the highest and lowest scores before averaging

<a name='M-ChuckDeviceController-Plugin-RequestTimings-Clone'></a>
### Clone() `method`

##### Summary

Clones an instance of a Timings class

##### Returns

Timings

##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugin-RequestTimings-Increment-System-Diagnostics-Stopwatch-'></a>
### Increment(stopWatch) `method`

##### Summary

Increments the total milliseconds

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stopWatch | [System.Diagnostics.Stopwatch](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Diagnostics.Stopwatch 'System.Diagnostics.Stopwatch') |  |

<a name='M-ChuckDeviceController-Plugin-RequestTimings-Increment-System-Int64-'></a>
### Increment(totalTicks) `method`

##### Summary

Increments the total ticks

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| totalTicks | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Total number of ticks to increment by. |

<a name='T-ChuckDeviceController-Plugin-SettingsProperty'></a>
## SettingsProperty `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Settings property interface contract used by plugins to
create UI setting elements in the dashboard.

<a name='M-ChuckDeviceController-Plugin-SettingsProperty-#ctor'></a>
### #ctor() `constructor`

##### Summary



##### Parameters

This constructor has no parameters.

<a name='M-ChuckDeviceController-Plugin-SettingsProperty-#ctor-System-String,System-String,ChuckDeviceController-Plugin-SettingsPropertyType,System-Object,System-Object,System-UInt32,System-Boolean,System-Boolean,System-String,System-String,ChuckDeviceController-Plugin-SettingsPropertyGroup-'></a>
### #ctor(text,name,type,value,defaultValue,displayIndex,isRequired,validate,className,style,group) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| text | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| type | [ChuckDeviceController.Plugin.SettingsPropertyType](#T-ChuckDeviceController-Plugin-SettingsPropertyType 'ChuckDeviceController.Plugin.SettingsPropertyType') |  |
| value | [System.Object](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object 'System.Object') |  |
| defaultValue | [System.Object](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object 'System.Object') |  |
| displayIndex | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |
| isRequired | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |
| validate | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |
| className | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| style | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| group | [ChuckDeviceController.Plugin.SettingsPropertyGroup](#T-ChuckDeviceController-Plugin-SettingsPropertyGroup 'ChuckDeviceController.Plugin.SettingsPropertyGroup') |  |

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-Class'></a>
### Class `property`

##### Summary

Gets or sets the CSS class name to use.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-DefaultValue'></a>
### DefaultValue `property`

##### Summary

Gets or sets the default value to use for the element, if
it supports it.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets a value used for sorting each HTML element
created for the properties.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-Group'></a>
### Group `property`

##### Summary

Gets or sets the group the settings property
will be in.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-IsRequired'></a>
### IsRequired `property`

##### Summary

Gets or sets a value determining whether the HTML element
value is required.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-Name'></a>
### Name `property`

##### Summary

Gets or sets the ID and name of the element.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-Style'></a>
### Style `property`

##### Summary

Gets or sets the raw CSS styling to use.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-Text'></a>
### Text `property`

##### Summary

Gets or sets the displayed text for the property, possibly
used in a label.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-Type'></a>
### Type `property`

##### Summary

Gets or sets the type of HTML element to create.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-Validate'></a>
### Validate `property`

##### Summary

Gets or sets a value determining whether to validate the
value of the HTML element.

<a name='P-ChuckDeviceController-Plugin-SettingsProperty-Value'></a>
### Value `property`

##### Summary

Gets or sets the initial value to set for the element.

<a name='T-ChuckDeviceController-Plugin-SettingsPropertyGroup'></a>
## SettingsPropertyGroup `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Interface contract for grouping settings properties.

<a name='M-ChuckDeviceController-Plugin-SettingsPropertyGroup-#ctor'></a>
### #ctor() `constructor`

##### Summary



##### Parameters

This constructor has no parameters.

<a name='M-ChuckDeviceController-Plugin-SettingsPropertyGroup-#ctor-System-String,System-String,System-UInt32-'></a>
### #ctor(id,text,displayIndex) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| text | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| displayIndex | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='P-ChuckDeviceController-Plugin-SettingsPropertyGroup-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets a value used for sorting each HTML element
created for the properties.

<a name='P-ChuckDeviceController-Plugin-SettingsPropertyGroup-Id'></a>
### Id `property`

##### Summary

Gets or sets the unique ID for the settings property group.

<a name='P-ChuckDeviceController-Plugin-SettingsPropertyGroup-Text'></a>
### Text `property`

##### Summary

Gets or sets the display text for the settings property group.

<a name='M-ChuckDeviceController-Plugin-SettingsPropertyGroup-Equals-System-Object-'></a>
### Equals(obj) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| obj | [System.Object](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object 'System.Object') |  |

<a name='M-ChuckDeviceController-Plugin-SettingsPropertyGroup-Equals-ChuckDeviceController-Plugin-SettingsPropertyGroup-'></a>
### Equals(other) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| other | [ChuckDeviceController.Plugin.SettingsPropertyGroup](#T-ChuckDeviceController-Plugin-SettingsPropertyGroup 'ChuckDeviceController.Plugin.SettingsPropertyGroup') |  |

<a name='M-ChuckDeviceController-Plugin-SettingsPropertyGroup-GetHashCode'></a>
### GetHashCode() `method`

##### Summary



##### Returns



##### Parameters

This method has no parameters.

<a name='T-ChuckDeviceController-Plugin-SettingsPropertyType'></a>
## SettingsPropertyType `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Determines the type of HTML element to create for
the settings property.

<a name='F-ChuckDeviceController-Plugin-SettingsPropertyType-CheckBox'></a>
### CheckBox `constants`

##### Summary

Settings property type is a checkbox field.

<a name='F-ChuckDeviceController-Plugin-SettingsPropertyType-Number'></a>
### Number `constants`

##### Summary

Settings property type is a numeric selector.

<a name='F-ChuckDeviceController-Plugin-SettingsPropertyType-Select'></a>
### Select `constants`

##### Summary

Settings property type is a select item list element.

<a name='F-ChuckDeviceController-Plugin-SettingsPropertyType-Text'></a>
### Text `constants`

##### Summary

Settings property type is a text field.

<a name='F-ChuckDeviceController-Plugin-SettingsPropertyType-TextArea'></a>
### TextArea `constants`

##### Summary

Settings property type is a text area.

<a name='T-ChuckDeviceController-Plugin-SettingsTab'></a>
## SettingsTab `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Settings tab interface contract to add UI settings for plugins.

<a name='M-ChuckDeviceController-Plugin-SettingsTab-#ctor'></a>
### #ctor() `constructor`

##### Summary



##### Parameters

This constructor has no parameters.

<a name='M-ChuckDeviceController-Plugin-SettingsTab-#ctor-System-String,System-String,System-String,System-UInt32,System-String,System-String-'></a>
### #ctor(id,text,anchor,displayIndex,className,style) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| text | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| anchor | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| displayIndex | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |
| className | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| style | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='P-ChuckDeviceController-Plugin-SettingsTab-Anchor'></a>
### Anchor `property`

##### Summary

Gets or sets the html anchor tag name of the tab.
Note: No hash symbol needed.

<a name='P-ChuckDeviceController-Plugin-SettingsTab-Class'></a>
### Class `property`

##### Summary

Gets or sets the CSS class name to use.

<a name='P-ChuckDeviceController-Plugin-SettingsTab-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the display index of the tab in the tab list.

<a name='P-ChuckDeviceController-Plugin-SettingsTab-Id'></a>
### Id `property`

##### Summary

Gets or sets the unique ID of the tab.

<a name='P-ChuckDeviceController-Plugin-SettingsTab-Style'></a>
### Style `property`

##### Summary

Gets or sets the raw CSS styling to use.

<a name='P-ChuckDeviceController-Plugin-SettingsTab-Text'></a>
### Text `property`

##### Summary

Gets or sets the display text of the tab.

<a name='T-ChuckDeviceController-Plugin-SidebarItem'></a>
## SidebarItem `type`

##### Namespace

ChuckDeviceController.Plugin

##### Summary

Navigation bar header plugin contract implementation.

<a name='M-ChuckDeviceController-Plugin-SidebarItem-#ctor'></a>
### #ctor() `constructor`

##### Summary

Instantiates a new navbar header instance using default 
property values.

##### Parameters

This constructor has no parameters.

<a name='M-ChuckDeviceController-Plugin-SidebarItem-#ctor-System-String,System-String,System-String,System-String,System-UInt32,System-Boolean,System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugin-SidebarItem},System-Boolean,System-Boolean-'></a>
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
| dropdownItems | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SidebarItem}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugin.SidebarItem}') |  |
| isDisabled | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |
| isSeparator | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |

<a name='P-ChuckDeviceController-Plugin-SidebarItem-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the navbar header is clicked.

<a name='P-ChuckDeviceController-Plugin-SidebarItem-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to.

<a name='P-ChuckDeviceController-Plugin-SidebarItem-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the numeric display index order of
the navbar header in the list of navbar headers.

<a name='P-ChuckDeviceController-Plugin-SidebarItem-DropdownItems'></a>
### DropdownItems `property`

##### Summary

Gets or sets a list of navbar header dropdown items.

<a name='P-ChuckDeviceController-Plugin-SidebarItem-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the FontAwesome v6 icon key to use for 
the navbar header. https://fontawesome.com/icons

<a name='P-ChuckDeviceController-Plugin-SidebarItem-IsDisabled'></a>
### IsDisabled `property`

##### Summary

Gets or sets a value determining whether the
navbar header is disabled or not.

<a name='P-ChuckDeviceController-Plugin-SidebarItem-IsDropdown'></a>
### IsDropdown `property`

##### Summary

Gets or sets a value determining whether the navbar
header should be treated as a dropdown.

<a name='P-ChuckDeviceController-Plugin-SidebarItem-IsSeparator'></a>
### IsSeparator `property`

##### Summary

Gets or sets a value determining whether to insert a
separator instead of a dropdown item.

<a name='P-ChuckDeviceController-Plugin-SidebarItem-Text'></a>
### Text `property`

##### Summary

Gets or sets the text to display for this navbar
header.

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

Determines where the static files (i.e. 'wwwroot' and 'Views') will be located
relevant to the plugin.

<a name='M-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-#ctor-ChuckDeviceController-Plugin-StaticFilesLocation,ChuckDeviceController-Plugin-StaticFilesLocation-'></a>
### #ctor(views,webRoot) `constructor`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| views | [ChuckDeviceController.Plugin.StaticFilesLocation](#T-ChuckDeviceController-Plugin-StaticFilesLocation 'ChuckDeviceController.Plugin.StaticFilesLocation') |  |
| webRoot | [ChuckDeviceController.Plugin.StaticFilesLocation](#T-ChuckDeviceController-Plugin-StaticFilesLocation 'ChuckDeviceController.Plugin.StaticFilesLocation') |  |

<a name='P-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-Views'></a>
### Views `property`

##### Summary

Gets an enum value determining where any Mvc Views are located.
i.e. 'Views' folder.

<a name='P-ChuckDeviceController-Plugin-StaticFilesLocationAttribute-WebRoot'></a>
### WebRoot `property`

##### Summary

Gets an enum value determining where any web resource files are
located. i.e. 'wwwroot' web root folder.
