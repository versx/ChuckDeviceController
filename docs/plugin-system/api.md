<a name='assembly'></a>
# Plugin API Reference

## Contents

- [DashboardStatsItem](#T-ChuckDeviceController-Plugins-DashboardStatsItem 'ChuckDeviceController.Plugins.DashboardStatsItem')
    - [#ctor(name,value,isHtml)](#M-ChuckDeviceController-Plugins-DashboardStatsItem-#ctor-System-String,System-String,System-Boolean- 'ChuckDeviceController.Plugins.DashboardStatsItem.#ctor(System.String,System.String,System.Boolean)')
    - [IsHtml](#P-ChuckDeviceController-Plugins-DashboardStatsItem-IsHtml 'ChuckDeviceController.Plugins.DashboardStatsItem.IsHtml')
    - [Name](#P-ChuckDeviceController-Plugins-DashboardStatsItem-Name 'ChuckDeviceController.Plugins.DashboardStatsItem.Name')
    - [Value](#P-ChuckDeviceController-Plugins-DashboardStatsItem-Value 'ChuckDeviceController.Plugins.DashboardStatsItem.Value')
- [DatabaseConnectionState](#T-ChuckDeviceController-Plugins-DatabaseConnectionState 'ChuckDeviceController.Plugins.DatabaseConnectionState')
    - [Connected](#F-ChuckDeviceController-Plugins-DatabaseConnectionState-Connected 'ChuckDeviceController.Plugins.DatabaseConnectionState.Connected')
    - [Disconnected](#F-ChuckDeviceController-Plugins-DatabaseConnectionState-Disconnected 'ChuckDeviceController.Plugins.DatabaseConnectionState.Disconnected')
- [IDashboardStatsItem](#T-ChuckDeviceController-Plugins-IDashboardStatsItem 'ChuckDeviceController.Plugins.IDashboardStatsItem')
    - [IsHtml](#P-ChuckDeviceController-Plugins-IDashboardStatsItem-IsHtml 'ChuckDeviceController.Plugins.IDashboardStatsItem.IsHtml')
    - [Name](#P-ChuckDeviceController-Plugins-IDashboardStatsItem-Name 'ChuckDeviceController.Plugins.IDashboardStatsItem.Name')
    - [Value](#P-ChuckDeviceController-Plugins-IDashboardStatsItem-Value 'ChuckDeviceController.Plugins.IDashboardStatsItem.Value')
- [IDatabaseEvents](#T-ChuckDeviceController-Plugins-IDatabaseEvents 'ChuckDeviceController.Plugins.IDatabaseEvents')
    - [OnEntityAdded\`\`1(entity)](#M-ChuckDeviceController-Plugins-IDatabaseEvents-OnEntityAdded``1-``0- 'ChuckDeviceController.Plugins.IDatabaseEvents.OnEntityAdded``1(``0)')
    - [OnEntityDeleted\`\`1(entity)](#M-ChuckDeviceController-Plugins-IDatabaseEvents-OnEntityDeleted``1-``0- 'ChuckDeviceController.Plugins.IDatabaseEvents.OnEntityDeleted``1(``0)')
    - [OnEntityModified\`\`1(oldEntity,newEntity)](#M-ChuckDeviceController-Plugins-IDatabaseEvents-OnEntityModified``1-``0,``0- 'ChuckDeviceController.Plugins.IDatabaseEvents.OnEntityModified``1(``0,``0)')
    - [OnStateChanged(state)](#M-ChuckDeviceController-Plugins-IDatabaseEvents-OnStateChanged-ChuckDeviceController-Plugins-DatabaseConnectionState- 'ChuckDeviceController.Plugins.IDatabaseEvents.OnStateChanged(ChuckDeviceController.Plugins.DatabaseConnectionState)')
- [IDatabaseHost](#T-ChuckDeviceController-Plugins-IDatabaseHost 'ChuckDeviceController.Plugins.IDatabaseHost')
    - [GetByIdAsync\`\`2(id)](#M-ChuckDeviceController-Plugins-IDatabaseHost-GetByIdAsync``2-``1- 'ChuckDeviceController.Plugins.IDatabaseHost.GetByIdAsync``2(``1)')
    - [GetListAsync\`\`1()](#M-ChuckDeviceController-Plugins-IDatabaseHost-GetListAsync``1 'ChuckDeviceController.Plugins.IDatabaseHost.GetListAsync``1')
- [IJobControllerServiceEvents](#T-ChuckDeviceController-Plugins-IJobControllerServiceEvents 'ChuckDeviceController.Plugins.IJobControllerServiceEvents')
- [IJobControllerServiceHost](#T-ChuckDeviceController-Plugins-IJobControllerServiceHost 'ChuckDeviceController.Plugins.IJobControllerServiceHost')
    - [AddJobControllerAsync(name,controller)](#M-ChuckDeviceController-Plugins-IJobControllerServiceHost-AddJobControllerAsync-System-String,ChuckDeviceController-Common-Jobs-IJobController- 'ChuckDeviceController.Plugins.IJobControllerServiceHost.AddJobControllerAsync(System.String,ChuckDeviceController.Common.Jobs.IJobController)')
    - [AssignDeviceToJobControllerAsync(device,jobControllerName)](#M-ChuckDeviceController-Plugins-IJobControllerServiceHost-AssignDeviceToJobControllerAsync-ChuckDeviceController-Common-Data-Contracts-IDevice,System-String- 'ChuckDeviceController.Plugins.IJobControllerServiceHost.AssignDeviceToJobControllerAsync(ChuckDeviceController.Common.Data.Contracts.IDevice,System.String)')
- [ILocalizationHost](#T-ChuckDeviceController-Plugins-ILocalizationHost 'ChuckDeviceController.Plugins.ILocalizationHost')
    - [GetAlignmentName(alignmentTypeId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetAlignmentName-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetAlignmentName(System.UInt32)')
    - [GetCharacterCategoryName(characterCategoryId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetCharacterCategoryName-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetCharacterCategoryName(System.UInt32)')
    - [GetCostumeName(costumeId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetCostumeName-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetCostumeName(System.UInt32)')
    - [GetEvolutionName(evolutionId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetEvolutionName-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetEvolutionName(System.UInt32)')
    - [GetFormName(formId,includeNormal)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetFormName-System-UInt32,System-Boolean- 'ChuckDeviceController.Plugins.ILocalizationHost.GetFormName(System.UInt32,System.Boolean)')
    - [GetGruntType(invasionCharacterId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetGruntType-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetGruntType(System.UInt32)')
    - [GetItem(itemId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetItem-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetItem(System.UInt32)')
    - [GetMoveName(moveId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetMoveName-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetMoveName(System.UInt32)')
    - [GetPokemonName(pokemonId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetPokemonName-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetPokemonName(System.UInt32)')
    - [GetThrowName(throwTypeId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetThrowName-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetThrowName(System.UInt32)')
    - [GetWeather(weatherConditionId)](#M-ChuckDeviceController-Plugins-ILocalizationHost-GetWeather-System-UInt32- 'ChuckDeviceController.Plugins.ILocalizationHost.GetWeather(System.UInt32)')
    - [Translate(key)](#M-ChuckDeviceController-Plugins-ILocalizationHost-Translate-System-String- 'ChuckDeviceController.Plugins.ILocalizationHost.Translate(System.String)')
    - [Translate(keyWithArgs,args)](#M-ChuckDeviceController-Plugins-ILocalizationHost-Translate-System-String,System-Object[]- 'ChuckDeviceController.Plugins.ILocalizationHost.Translate(System.String,System.Object[])')
- [ILoggingHost](#T-ChuckDeviceController-Plugins-ILoggingHost 'ChuckDeviceController.Plugins.ILoggingHost')
    - [LogException(ex)](#M-ChuckDeviceController-Plugins-ILoggingHost-LogException-System-Exception- 'ChuckDeviceController.Plugins.ILoggingHost.LogException(System.Exception)')
    - [LogMessage(text,args)](#M-ChuckDeviceController-Plugins-ILoggingHost-LogMessage-System-String,System-Object[]- 'ChuckDeviceController.Plugins.ILoggingHost.LogMessage(System.String,System.Object[])')
- [IMetadata](#T-ChuckDeviceController-Plugins-IMetadata 'ChuckDeviceController.Plugins.IMetadata')
    - [Author](#P-ChuckDeviceController-Plugins-IMetadata-Author 'ChuckDeviceController.Plugins.IMetadata.Author')
    - [Description](#P-ChuckDeviceController-Plugins-IMetadata-Description 'ChuckDeviceController.Plugins.IMetadata.Description')
    - [Name](#P-ChuckDeviceController-Plugins-IMetadata-Name 'ChuckDeviceController.Plugins.IMetadata.Name')
    - [Version](#P-ChuckDeviceController-Plugins-IMetadata-Version 'ChuckDeviceController.Plugins.IMetadata.Version')
- [INavbarHeader](#T-ChuckDeviceController-Plugins-INavbarHeader 'ChuckDeviceController.Plugins.INavbarHeader')
    - [ActionName](#P-ChuckDeviceController-Plugins-INavbarHeader-ActionName 'ChuckDeviceController.Plugins.INavbarHeader.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugins-INavbarHeader-ControllerName 'ChuckDeviceController.Plugins.INavbarHeader.ControllerName')
    - [DisplayIndex](#P-ChuckDeviceController-Plugins-INavbarHeader-DisplayIndex 'ChuckDeviceController.Plugins.INavbarHeader.DisplayIndex')
    - [Icon](#P-ChuckDeviceController-Plugins-INavbarHeader-Icon 'ChuckDeviceController.Plugins.INavbarHeader.Icon')
    - [IsDisabled](#P-ChuckDeviceController-Plugins-INavbarHeader-IsDisabled 'ChuckDeviceController.Plugins.INavbarHeader.IsDisabled')
    - [Text](#P-ChuckDeviceController-Plugins-INavbarHeader-Text 'ChuckDeviceController.Plugins.INavbarHeader.Text')
- [IPlugin](#T-ChuckDeviceController-Plugins-IPlugin 'ChuckDeviceController.Plugins.IPlugin')
- [IPluginEvents](#T-ChuckDeviceController-Plugins-IPluginEvents 'ChuckDeviceController.Plugins.IPluginEvents')
    - [OnLoad()](#M-ChuckDeviceController-Plugins-IPluginEvents-OnLoad 'ChuckDeviceController.Plugins.IPluginEvents.OnLoad')
    - [OnReload()](#M-ChuckDeviceController-Plugins-IPluginEvents-OnReload 'ChuckDeviceController.Plugins.IPluginEvents.OnReload')
    - [OnRemove()](#M-ChuckDeviceController-Plugins-IPluginEvents-OnRemove 'ChuckDeviceController.Plugins.IPluginEvents.OnRemove')
    - [OnStateChanged(state,isEnabled)](#M-ChuckDeviceController-Plugins-IPluginEvents-OnStateChanged-ChuckDeviceController-Plugins-PluginState,System-Boolean- 'ChuckDeviceController.Plugins.IPluginEvents.OnStateChanged(ChuckDeviceController.Plugins.PluginState,System.Boolean)')
    - [OnStop()](#M-ChuckDeviceController-Plugins-IPluginEvents-OnStop 'ChuckDeviceController.Plugins.IPluginEvents.OnStop')
- [IRepository\`2](#T-ChuckDeviceController-Plugins-Data-IRepository`2 'ChuckDeviceController.Plugins.Data.IRepository`2')
    - [GetByIdAsync(id)](#M-ChuckDeviceController-Plugins-Data-IRepository`2-GetByIdAsync-`1- 'ChuckDeviceController.Plugins.Data.IRepository`2.GetByIdAsync(`1)')
    - [GetListAsync()](#M-ChuckDeviceController-Plugins-Data-IRepository`2-GetListAsync 'ChuckDeviceController.Plugins.Data.IRepository`2.GetListAsync')
- [IUiEvents](#T-ChuckDeviceController-Plugins-IUiEvents 'ChuckDeviceController.Plugins.IUiEvents')
- [IUiHost](#T-ChuckDeviceController-Plugins-IUiHost 'ChuckDeviceController.Plugins.IUiHost')
    - [DashboardStatsItems](#P-ChuckDeviceController-Plugins-IUiHost-DashboardStatsItems 'ChuckDeviceController.Plugins.IUiHost.DashboardStatsItems')
    - [NavbarHeaders](#P-ChuckDeviceController-Plugins-IUiHost-NavbarHeaders 'ChuckDeviceController.Plugins.IUiHost.NavbarHeaders')
    - [AddDashboardStatisticAsync(stat)](#M-ChuckDeviceController-Plugins-IUiHost-AddDashboardStatisticAsync-ChuckDeviceController-Plugins-IDashboardStatsItem- 'ChuckDeviceController.Plugins.IUiHost.AddDashboardStatisticAsync(ChuckDeviceController.Plugins.IDashboardStatsItem)')
    - [AddDashboardStatisticsAsync(stats)](#M-ChuckDeviceController-Plugins-IUiHost-AddDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugins-IDashboardStatsItem}- 'ChuckDeviceController.Plugins.IUiHost.AddDashboardStatisticsAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.IDashboardStatsItem})')
    - [AddNavbarHeaderAsync(header)](#M-ChuckDeviceController-Plugins-IUiHost-AddNavbarHeaderAsync-ChuckDeviceController-Plugins-NavbarHeader- 'ChuckDeviceController.Plugins.IUiHost.AddNavbarHeaderAsync(ChuckDeviceController.Plugins.NavbarHeader)')
    - [AddNavbarHeadersAsync(headers)](#M-ChuckDeviceController-Plugins-IUiHost-AddNavbarHeadersAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugins-NavbarHeader}- 'ChuckDeviceController.Plugins.IUiHost.AddNavbarHeadersAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.NavbarHeader})')
    - [UpdateDashboardStatisticAsync(stat)](#M-ChuckDeviceController-Plugins-IUiHost-UpdateDashboardStatisticAsync-ChuckDeviceController-Plugins-IDashboardStatsItem- 'ChuckDeviceController.Plugins.IUiHost.UpdateDashboardStatisticAsync(ChuckDeviceController.Plugins.IDashboardStatsItem)')
    - [UpdateDashboardStatisticsAsync(stats)](#M-ChuckDeviceController-Plugins-IUiHost-UpdateDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugins-IDashboardStatsItem}- 'ChuckDeviceController.Plugins.IUiHost.UpdateDashboardStatisticsAsync(System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.IDashboardStatsItem})')
- [IWebPlugin](#T-ChuckDeviceController-Plugins-IWebPlugin 'ChuckDeviceController.Plugins.IWebPlugin')
    - [Configure(appBuilder)](#M-ChuckDeviceController-Plugins-IWebPlugin-Configure-Microsoft-AspNetCore-Builder-IApplicationBuilder- 'ChuckDeviceController.Plugins.IWebPlugin.Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder)')
    - [ConfigureServices(services)](#M-ChuckDeviceController-Plugins-IWebPlugin-ConfigureServices-Microsoft-Extensions-DependencyInjection-IServiceCollection- 'ChuckDeviceController.Plugins.IWebPlugin.ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection)')
- [NavbarHeader](#T-ChuckDeviceController-Plugins-NavbarHeader 'ChuckDeviceController.Plugins.NavbarHeader')
    - [#ctor()](#M-ChuckDeviceController-Plugins-NavbarHeader-#ctor 'ChuckDeviceController.Plugins.NavbarHeader.#ctor')
    - [#ctor(text,controllerName,actionName,icon,displayIndex,isDropdown,dropdownItems,isDisabled)](#M-ChuckDeviceController-Plugins-NavbarHeader-#ctor-System-String,System-String,System-String,System-String,System-UInt32,System-Boolean,System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugins-NavbarHeaderDropdownItem},System-Boolean- 'ChuckDeviceController.Plugins.NavbarHeader.#ctor(System.String,System.String,System.String,System.String,System.UInt32,System.Boolean,System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.NavbarHeaderDropdownItem},System.Boolean)')
    - [ActionName](#P-ChuckDeviceController-Plugins-NavbarHeader-ActionName 'ChuckDeviceController.Plugins.NavbarHeader.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugins-NavbarHeader-ControllerName 'ChuckDeviceController.Plugins.NavbarHeader.ControllerName')
    - [DisplayIndex](#P-ChuckDeviceController-Plugins-NavbarHeader-DisplayIndex 'ChuckDeviceController.Plugins.NavbarHeader.DisplayIndex')
    - [DropdownItems](#P-ChuckDeviceController-Plugins-NavbarHeader-DropdownItems 'ChuckDeviceController.Plugins.NavbarHeader.DropdownItems')
    - [Icon](#P-ChuckDeviceController-Plugins-NavbarHeader-Icon 'ChuckDeviceController.Plugins.NavbarHeader.Icon')
    - [IsDisabled](#P-ChuckDeviceController-Plugins-NavbarHeader-IsDisabled 'ChuckDeviceController.Plugins.NavbarHeader.IsDisabled')
    - [IsDropdown](#P-ChuckDeviceController-Plugins-NavbarHeader-IsDropdown 'ChuckDeviceController.Plugins.NavbarHeader.IsDropdown')
    - [Text](#P-ChuckDeviceController-Plugins-NavbarHeader-Text 'ChuckDeviceController.Plugins.NavbarHeader.Text')
- [NavbarHeaderDropdownItem](#T-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem')
    - [#ctor(text,controllerName,actionName,icon,displayIndex,isSeparator,isDisabled)](#M-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-#ctor-System-String,System-String,System-String,System-String,System-UInt32,System-Boolean,System-Boolean- 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem.#ctor(System.String,System.String,System.String,System.String,System.UInt32,System.Boolean,System.Boolean)')
    - [ActionName](#P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-ActionName 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem.ActionName')
    - [ControllerName](#P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-ControllerName 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem.ControllerName')
    - [DisplayIndex](#P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-DisplayIndex 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem.DisplayIndex')
    - [Icon](#P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-Icon 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem.Icon')
    - [IsDisabled](#P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-IsDisabled 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem.IsDisabled')
    - [IsSeparator](#P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-IsSeparator 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem.IsSeparator')
    - [Text](#P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-Text 'ChuckDeviceController.Plugins.NavbarHeaderDropdownItem.Text')
- [PluginPermissions](#T-ChuckDeviceController-Plugins-PluginPermissions 'ChuckDeviceController.Plugins.PluginPermissions')
    - [AddControllers](#F-ChuckDeviceController-Plugins-PluginPermissions-AddControllers 'ChuckDeviceController.Plugins.PluginPermissions.AddControllers')
    - [AddInstances](#F-ChuckDeviceController-Plugins-PluginPermissions-AddInstances 'ChuckDeviceController.Plugins.PluginPermissions.AddInstances')
    - [AddJobControllers](#F-ChuckDeviceController-Plugins-PluginPermissions-AddJobControllers 'ChuckDeviceController.Plugins.PluginPermissions.AddJobControllers')
    - [All](#F-ChuckDeviceController-Plugins-PluginPermissions-All 'ChuckDeviceController.Plugins.PluginPermissions.All')
    - [DeleteDatabase](#F-ChuckDeviceController-Plugins-PluginPermissions-DeleteDatabase 'ChuckDeviceController.Plugins.PluginPermissions.DeleteDatabase')
    - [None](#F-ChuckDeviceController-Plugins-PluginPermissions-None 'ChuckDeviceController.Plugins.PluginPermissions.None')
    - [ReadDatabase](#F-ChuckDeviceController-Plugins-PluginPermissions-ReadDatabase 'ChuckDeviceController.Plugins.PluginPermissions.ReadDatabase')
    - [WriteDatabase](#F-ChuckDeviceController-Plugins-PluginPermissions-WriteDatabase 'ChuckDeviceController.Plugins.PluginPermissions.WriteDatabase')
- [PluginPermissionsAttribute](#T-ChuckDeviceController-Plugins-PluginPermissionsAttribute 'ChuckDeviceController.Plugins.PluginPermissionsAttribute')
    - [#ctor(permissions)](#M-ChuckDeviceController-Plugins-PluginPermissionsAttribute-#ctor-ChuckDeviceController-Plugins-PluginPermissions- 'ChuckDeviceController.Plugins.PluginPermissionsAttribute.#ctor(ChuckDeviceController.Plugins.PluginPermissions)')
    - [Permissions](#P-ChuckDeviceController-Plugins-PluginPermissionsAttribute-Permissions 'ChuckDeviceController.Plugins.PluginPermissionsAttribute.Permissions')
- [PluginState](#T-ChuckDeviceController-Plugins-PluginState 'ChuckDeviceController.Plugins.PluginState')
    - [Disabled](#F-ChuckDeviceController-Plugins-PluginState-Disabled 'ChuckDeviceController.Plugins.PluginState.Disabled')
    - [Error](#F-ChuckDeviceController-Plugins-PluginState-Error 'ChuckDeviceController.Plugins.PluginState.Error')
    - [Removed](#F-ChuckDeviceController-Plugins-PluginState-Removed 'ChuckDeviceController.Plugins.PluginState.Removed')
    - [Running](#F-ChuckDeviceController-Plugins-PluginState-Running 'ChuckDeviceController.Plugins.PluginState.Running')
    - [Stopped](#F-ChuckDeviceController-Plugins-PluginState-Stopped 'ChuckDeviceController.Plugins.PluginState.Stopped')
    - [Unset](#F-ChuckDeviceController-Plugins-PluginState-Unset 'ChuckDeviceController.Plugins.PluginState.Unset')

<a name='T-ChuckDeviceController-Plugins-DashboardStatsItem'></a>
## DashboardStatsItem `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Dashboard statistics item for displaying information
on the front page.

<a name='M-ChuckDeviceController-Plugins-DashboardStatsItem-#ctor-System-String,System-String,System-Boolean-'></a>
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

<a name='P-ChuckDeviceController-Plugins-DashboardStatsItem-IsHtml'></a>
### IsHtml `property`

##### Summary

Gets or sets a value determining whether the name
and value properties include raw HTML or not.

<a name='P-ChuckDeviceController-Plugins-DashboardStatsItem-Name'></a>
### Name `property`

##### Summary

Gets or sets the name or title of the statistic.

<a name='P-ChuckDeviceController-Plugins-DashboardStatsItem-Value'></a>
### Value `property`

##### Summary

Gets or sets the value of the statistic.

<a name='T-ChuckDeviceController-Plugins-DatabaseConnectionState'></a>
## DatabaseConnectionState `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Enumeration of possible database connection states.

<a name='F-ChuckDeviceController-Plugins-DatabaseConnectionState-Connected'></a>
### Connected `constants`

##### Summary

Database is in the connected state.

<a name='F-ChuckDeviceController-Plugins-DatabaseConnectionState-Disconnected'></a>
### Disconnected `constants`

##### Summary

Database is in the disconnected state.

<a name='T-ChuckDeviceController-Plugins-IDashboardStatsItem'></a>
## IDashboardStatsItem `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Dashboard statistics item for displaying information
on the front page.

<a name='P-ChuckDeviceController-Plugins-IDashboardStatsItem-IsHtml'></a>
### IsHtml `property`

##### Summary

Gets or sets a value determining whether the name
and value properties include raw HTML or not.

<a name='P-ChuckDeviceController-Plugins-IDashboardStatsItem-Name'></a>
### Name `property`

##### Summary

Gets or sets the name or title of the statistic.

<a name='P-ChuckDeviceController-Plugins-IDashboardStatsItem-Value'></a>
### Value `property`

##### Summary

Gets or sets the value of the statistic.

<a name='T-ChuckDeviceController-Plugins-IDatabaseEvents'></a>
## IDatabaseEvents `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Provides delegates of database related events from
the host application.

<a name='M-ChuckDeviceController-Plugins-IDatabaseEvents-OnEntityAdded``1-``0-'></a>
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

<a name='M-ChuckDeviceController-Plugins-IDatabaseEvents-OnEntityDeleted``1-``0-'></a>
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

<a name='M-ChuckDeviceController-Plugins-IDatabaseEvents-OnEntityModified``1-``0,``0-'></a>
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

<a name='M-ChuckDeviceController-Plugins-IDatabaseEvents-OnStateChanged-ChuckDeviceController-Plugins-DatabaseConnectionState-'></a>
### OnStateChanged(state) `method`

##### Summary

Called when the state of the database has changed.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| state | [ChuckDeviceController.Plugins.DatabaseConnectionState](#T-ChuckDeviceController-Plugins-DatabaseConnectionState 'ChuckDeviceController.Plugins.DatabaseConnectionState') | Current state of the database connection. |

<a name='T-ChuckDeviceController-Plugins-IDatabaseHost'></a>
## IDatabaseHost `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Plugin host handler contract used to interact with the database entities.

<a name='M-ChuckDeviceController-Plugins-IDatabaseHost-GetByIdAsync``2-``1-'></a>
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

<a name='M-ChuckDeviceController-Plugins-IDatabaseHost-GetListAsync``1'></a>
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

<a name='T-ChuckDeviceController-Plugins-IJobControllerServiceEvents'></a>
## IJobControllerServiceEvents `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Job controller service related events that have occurred
in the host application.

<a name='T-ChuckDeviceController-Plugins-IJobControllerServiceHost'></a>
## IJobControllerServiceHost `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Plugin host handler contract used to interact with and manage the
job controller service.

<a name='M-ChuckDeviceController-Plugins-IJobControllerServiceHost-AddJobControllerAsync-System-String,ChuckDeviceController-Common-Jobs-IJobController-'></a>
### AddJobControllerAsync(name,controller) `method`

##### Summary



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| controller | [ChuckDeviceController.Common.Jobs.IJobController](#T-ChuckDeviceController-Common-Jobs-IJobController 'ChuckDeviceController.Common.Jobs.IJobController') |  |

<a name='M-ChuckDeviceController-Plugins-IJobControllerServiceHost-AssignDeviceToJobControllerAsync-ChuckDeviceController-Common-Data-Contracts-IDevice,System-String-'></a>
### AssignDeviceToJobControllerAsync(device,jobControllerName) `method`

##### Summary

Assigns the specified device to a specific job controller
instance by name.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| device | [ChuckDeviceController.Common.Data.Contracts.IDevice](#T-ChuckDeviceController-Common-Data-Contracts-IDevice 'ChuckDeviceController.Common.Data.Contracts.IDevice') | Device entity. |
| jobControllerName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Job controller instance name. |

<a name='T-ChuckDeviceController-Plugins-ILocalizationHost'></a>
## ILocalizationHost `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Plugin host handler contract used to translate strings.

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetAlignmentName-System-UInt32-'></a>
### GetAlignmentName(alignmentTypeId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| alignmentTypeId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetCharacterCategoryName-System-UInt32-'></a>
### GetCharacterCategoryName(characterCategoryId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| characterCategoryId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetCostumeName-System-UInt32-'></a>
### GetCostumeName(costumeId) `method`

##### Summary

Translate a Pokemon costume id to name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| costumeId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Costume ID to translate to name. |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetEvolutionName-System-UInt32-'></a>
### GetEvolutionName(evolutionId) `method`

##### Summary

Translate a Pokemon evolution id to name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| evolutionId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Evolution ID to translate to name. |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetFormName-System-UInt32,System-Boolean-'></a>
### GetFormName(formId,includeNormal) `method`

##### Summary

Translate a Pokemon form id to name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| formId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Form ID to translate to name. |
| includeNormal | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Include 'Normal' form name or not. |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetGruntType-System-UInt32-'></a>
### GetGruntType(invasionCharacterId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| invasionCharacterId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetItem-System-UInt32-'></a>
### GetItem(itemId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| itemId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetMoveName-System-UInt32-'></a>
### GetMoveName(moveId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| moveId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetPokemonName-System-UInt32-'></a>
### GetPokemonName(pokemonId) `method`

##### Summary

Translate a Pokemon id to name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| pokemonId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') | Pokemon ID to translate to name. |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetThrowName-System-UInt32-'></a>
### GetThrowName(throwTypeId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| throwTypeId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-GetWeather-System-UInt32-'></a>
### GetWeather(weatherConditionId) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| weatherConditionId | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-Translate-System-String-'></a>
### Translate(key) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| key | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='M-ChuckDeviceController-Plugins-ILocalizationHost-Translate-System-String,System-Object[]-'></a>
### Translate(keyWithArgs,args) `method`

##### Summary



##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| keyWithArgs | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| args | [System.Object[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object[] 'System.Object[]') |  |

<a name='T-ChuckDeviceController-Plugins-ILoggingHost'></a>
## ILoggingHost `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Plugin host handler for logging messages from plugins.

<a name='M-ChuckDeviceController-Plugins-ILoggingHost-LogException-System-Exception-'></a>
### LogException(ex) `method`

##### Summary

Log an exception that has been thrown to the
host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| ex | [System.Exception](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Exception 'System.Exception') | Exception that was thrown. |

<a name='M-ChuckDeviceController-Plugins-ILoggingHost-LogMessage-System-String,System-Object[]-'></a>
### LogMessage(text,args) `method`

##### Summary

Log a message to the host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| text | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Formatted log message string. |
| args | [System.Object[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object[] 'System.Object[]') | Arguments to parse with log message. |

<a name='T-ChuckDeviceController-Plugins-IMetadata'></a>
## IMetadata `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Plugin metadata details.

<a name='P-ChuckDeviceController-Plugins-IMetadata-Author'></a>
### Author `property`

##### Summary

Gets or sets the creator/author name that wrote the Plugin.

<a name='P-ChuckDeviceController-Plugins-IMetadata-Description'></a>
### Description `property`

##### Summary

Gets or sets the description about the Plugin.

<a name='P-ChuckDeviceController-Plugins-IMetadata-Name'></a>
### Name `property`

##### Summary

Gets or sets the name of the Plugin.

<a name='P-ChuckDeviceController-Plugins-IMetadata-Version'></a>
### Version `property`

##### Summary

Gets or sets the current version of the Plugin.

<a name='T-ChuckDeviceController-Plugins-INavbarHeader'></a>
## INavbarHeader `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Navigation bar header plugin contract.

<a name='P-ChuckDeviceController-Plugins-INavbarHeader-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the navbar header is selected.

<a name='P-ChuckDeviceController-Plugins-INavbarHeader-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to.

<a name='P-ChuckDeviceController-Plugins-INavbarHeader-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the numeric display index order of
the navbar header in the list of navbar headers.

<a name='P-ChuckDeviceController-Plugins-INavbarHeader-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the FontAwesome v6 icon key to use for 
the navbar header. https://fontawesome.com/icons

<a name='P-ChuckDeviceController-Plugins-INavbarHeader-IsDisabled'></a>
### IsDisabled `property`

##### Summary

Gets or sets a value determining whether the
navbar header is disabled or not.

<a name='P-ChuckDeviceController-Plugins-INavbarHeader-Text'></a>
### Text `property`

##### Summary

Gets or sets the text to display for this navbar
header.

<a name='T-ChuckDeviceController-Plugins-IPlugin'></a>
## IPlugin `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Base Plugin interface contract all plugins will inherit
at a minimum.

<a name='T-ChuckDeviceController-Plugins-IPluginEvents'></a>
## IPluginEvents `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Provides delegates of plugin related events
from the host application.

<a name='M-ChuckDeviceController-Plugins-IPluginEvents-OnLoad'></a>
### OnLoad() `method`

##### Summary

Called when the plugin has been fully loaded
and initialized from the host application.

##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugins-IPluginEvents-OnReload'></a>
### OnReload() `method`

##### Summary

Called when the plugin has been reloaded
by the host application.

##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugins-IPluginEvents-OnRemove'></a>
### OnRemove() `method`

##### Summary

Called when the plugin has been removed by
the host application.

##### Parameters

This method has no parameters.

<a name='M-ChuckDeviceController-Plugins-IPluginEvents-OnStateChanged-ChuckDeviceController-Plugins-PluginState,System-Boolean-'></a>
### OnStateChanged(state,isEnabled) `method`

##### Summary

Called when the plugin's state has been
changed by the host application.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| state | [ChuckDeviceController.Plugins.PluginState](#T-ChuckDeviceController-Plugins-PluginState 'ChuckDeviceController.Plugins.PluginState') | Plugin's current state |
| isEnabled | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | Whether the plugin is
currently enabled or disabled |

<a name='M-ChuckDeviceController-Plugins-IPluginEvents-OnStop'></a>
### OnStop() `method`

##### Summary

Called when the plugin has been stopped by
the host application.

##### Parameters

This method has no parameters.

<a name='T-ChuckDeviceController-Plugins-Data-IRepository`2'></a>
## IRepository\`2 `type`

##### Namespace

ChuckDeviceController.Plugins.Data

##### Summary

Repository contract for specific database entity types.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TEntity | Database entity contract type. |
| TId | Database entity primary key type. |

<a name='M-ChuckDeviceController-Plugins-Data-IRepository`2-GetByIdAsync-`1-'></a>
### GetByIdAsync(id) `method`

##### Summary

Gets a database entity by primary key.

##### Returns

Returns a database entity.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [\`1](#T-`1 '`1') | Primary key of the database entity. |

<a name='M-ChuckDeviceController-Plugins-Data-IRepository`2-GetListAsync'></a>
### GetListAsync() `method`

##### Summary

Gets a list of database entities.

##### Returns

Returns a list of database entities.

##### Parameters

This method has no parameters.

<a name='T-ChuckDeviceController-Plugins-IUiEvents'></a>
## IUiEvents `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

UI related events that have occurred in
the host application.

<a name='T-ChuckDeviceController-Plugins-IUiHost'></a>
## IUiHost `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Plugin host handler for executing user interface operations.

<a name='P-ChuckDeviceController-Plugins-IUiHost-DashboardStatsItems'></a>
### DashboardStatsItems `property`

##### Summary

Gets a list of dashboard statistics registered by plugins.

<a name='P-ChuckDeviceController-Plugins-IUiHost-NavbarHeaders'></a>
### NavbarHeaders `property`

##### Summary

Gets a list of navbar headers registered by plugins.

<a name='M-ChuckDeviceController-Plugins-IUiHost-AddDashboardStatisticAsync-ChuckDeviceController-Plugins-IDashboardStatsItem-'></a>
### AddDashboardStatisticAsync(stat) `method`

##### Summary

Adds a custom to the
dashboard front page.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stat | [ChuckDeviceController.Plugins.IDashboardStatsItem](#T-ChuckDeviceController-Plugins-IDashboardStatsItem 'ChuckDeviceController.Plugins.IDashboardStatsItem') | Dashboard statistics item to add. |

<a name='M-ChuckDeviceController-Plugins-IUiHost-AddDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugins-IDashboardStatsItem}-'></a>
### AddDashboardStatisticsAsync(stats) `method`

##### Summary

Adds a list of items to
the dashboard front page.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stats | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.IDashboardStatsItem}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.IDashboardStatsItem}') | List of dashboard statistic items to add. |

<a name='M-ChuckDeviceController-Plugins-IUiHost-AddNavbarHeaderAsync-ChuckDeviceController-Plugins-NavbarHeader-'></a>
### AddNavbarHeaderAsync(header) `method`

##### Summary

Adds a item to the main
application's Mvc navbar header.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| header | [ChuckDeviceController.Plugins.NavbarHeader](#T-ChuckDeviceController-Plugins-NavbarHeader 'ChuckDeviceController.Plugins.NavbarHeader') | Navbar to add. |

<a name='M-ChuckDeviceController-Plugins-IUiHost-AddNavbarHeadersAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugins-NavbarHeader}-'></a>
### AddNavbarHeadersAsync(headers) `method`

##### Summary

Adds a list of items to the
main application's Mvc navbar header.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| headers | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.NavbarHeader}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.NavbarHeader}') | List of navbars to add. |

<a name='M-ChuckDeviceController-Plugins-IUiHost-UpdateDashboardStatisticAsync-ChuckDeviceController-Plugins-IDashboardStatsItem-'></a>
### UpdateDashboardStatisticAsync(stat) `method`

##### Summary

Update an existing item
on the dashboard front page.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stat | [ChuckDeviceController.Plugins.IDashboardStatsItem](#T-ChuckDeviceController-Plugins-IDashboardStatsItem 'ChuckDeviceController.Plugins.IDashboardStatsItem') | Dashboard statistics item to update. |

<a name='M-ChuckDeviceController-Plugins-IUiHost-UpdateDashboardStatisticsAsync-System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugins-IDashboardStatsItem}-'></a>
### UpdateDashboardStatisticsAsync(stats) `method`

##### Summary

Update a list of existing items
on the dashboard front page.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| stats | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.IDashboardStatsItem}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.IDashboardStatsItem}') | List of dashboard statistic items to update. |

<a name='T-ChuckDeviceController-Plugins-IWebPlugin'></a>
## IWebPlugin `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Interface contract allowing Mvc services registration and configuration

<a name='M-ChuckDeviceController-Plugins-IWebPlugin-Configure-Microsoft-AspNetCore-Builder-IApplicationBuilder-'></a>
### Configure(appBuilder) `method`

##### Summary

Configures the application to set up middlewares, routing rules, etc.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| appBuilder | [Microsoft.AspNetCore.Builder.IApplicationBuilder](#T-Microsoft-AspNetCore-Builder-IApplicationBuilder 'Microsoft.AspNetCore.Builder.IApplicationBuilder') | Provides the mechanisms to configure an application's request pipeline. |

<a name='M-ChuckDeviceController-Plugins-IWebPlugin-ConfigureServices-Microsoft-Extensions-DependencyInjection-IServiceCollection-'></a>
### ConfigureServices(services) `method`

##### Summary

Register services into the IServiceCollection to use with Dependency Injection.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| services | [Microsoft.Extensions.DependencyInjection.IServiceCollection](#T-Microsoft-Extensions-DependencyInjection-IServiceCollection 'Microsoft.Extensions.DependencyInjection.IServiceCollection') | Specifies the contract for a collection of service descriptors. |

<a name='T-ChuckDeviceController-Plugins-NavbarHeader'></a>
## NavbarHeader `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Navigation bar header plugin contract implementation.

<a name='M-ChuckDeviceController-Plugins-NavbarHeader-#ctor'></a>
### #ctor() `constructor`

##### Summary

Instantiates a new navbar header instance using default 
property values.

##### Parameters

This constructor has no parameters.

<a name='M-ChuckDeviceController-Plugins-NavbarHeader-#ctor-System-String,System-String,System-String,System-String,System-UInt32,System-Boolean,System-Collections-Generic-IEnumerable{ChuckDeviceController-Plugins-NavbarHeaderDropdownItem},System-Boolean-'></a>
### #ctor(text,controllerName,actionName,icon,displayIndex,isDropdown,dropdownItems,isDisabled) `constructor`

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
| dropdownItems | [System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.NavbarHeaderDropdownItem}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{ChuckDeviceController.Plugins.NavbarHeaderDropdownItem}') |  |
| isDisabled | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |

<a name='P-ChuckDeviceController-Plugins-NavbarHeader-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the navbar header is selected.

<a name='P-ChuckDeviceController-Plugins-NavbarHeader-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to.

<a name='P-ChuckDeviceController-Plugins-NavbarHeader-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the numeric display index order of
the navbar header in the list of navbar headers.

<a name='P-ChuckDeviceController-Plugins-NavbarHeader-DropdownItems'></a>
### DropdownItems `property`

##### Summary

Gets or sets a list of navbar header dropdown items.

<a name='P-ChuckDeviceController-Plugins-NavbarHeader-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the FontAwesome v6 icon key to use for 
the navbar header. https://fontawesome.com/icons

<a name='P-ChuckDeviceController-Plugins-NavbarHeader-IsDisabled'></a>
### IsDisabled `property`

##### Summary

Gets or sets a value determining whether the
navbar header is disabled or not.

<a name='P-ChuckDeviceController-Plugins-NavbarHeader-IsDropdown'></a>
### IsDropdown `property`

##### Summary

Gets or sets a value determining whether the navbar
header should be treated as a dropdown.

<a name='P-ChuckDeviceController-Plugins-NavbarHeader-Text'></a>
### Text `property`

##### Summary

Gets or sets the text to display for this navbar
header.

<a name='T-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem'></a>
## NavbarHeaderDropdownItem `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Navigation bar header dropdown item plugin contract implemention.

<a name='M-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-#ctor-System-String,System-String,System-String,System-String,System-UInt32,System-Boolean,System-Boolean-'></a>
### #ctor(text,controllerName,actionName,icon,displayIndex,isSeparator,isDisabled) `constructor`

##### Summary

Instantiates a new instance of a navbar header with
dropdown items.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| text | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| controllerName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| actionName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| icon | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |
| displayIndex | [System.UInt32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.UInt32 'System.UInt32') |  |
| isSeparator | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |
| isDisabled | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') |  |

<a name='P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-ActionName'></a>
### ActionName `property`

##### Summary

Gets or sets the controller action name to execute
when the navbar header is selected.

<a name='P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-ControllerName'></a>
### ControllerName `property`

##### Summary

Gets or sets the controller name the action name
should relate to.

<a name='P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-DisplayIndex'></a>
### DisplayIndex `property`

##### Summary

Gets or sets the numeric display index order of
the navbar header in the list of navbar headers.

<a name='P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-Icon'></a>
### Icon `property`

##### Summary

Gets or sets the FontAwesome v6 icon key to use for 
the navbar header dropdown item. https://fontawesome.com/icons

<a name='P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-IsDisabled'></a>
### IsDisabled `property`

##### Summary

Gets or sets a value determining whether the navbar
header dropdown item is disabled or not.

<a name='P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-IsSeparator'></a>
### IsSeparator `property`

##### Summary

Gets or sets a value determining whether to insert a dropdown
separator instead of a dropdown item.

<a name='P-ChuckDeviceController-Plugins-NavbarHeaderDropdownItem-Text'></a>
### Text `property`

##### Summary

Gets or sets the text to display for this navbar
header.

<a name='T-ChuckDeviceController-Plugins-PluginPermissions'></a>
## PluginPermissions `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Enumeration of available permissions a plugin can request.

<a name='F-ChuckDeviceController-Plugins-PluginPermissions-AddControllers'></a>
### AddControllers `constants`

##### Summary

Add new ASP.NET Mvc controller routes

<a name='F-ChuckDeviceController-Plugins-PluginPermissions-AddInstances'></a>
### AddInstances `constants`

##### Summary

Add new instances

<a name='F-ChuckDeviceController-Plugins-PluginPermissions-AddJobControllers'></a>
### AddJobControllers `constants`

##### Summary

Add new job controller instances for devices

<a name='F-ChuckDeviceController-Plugins-PluginPermissions-All'></a>
### All `constants`

##### Summary

All available permissions

<a name='F-ChuckDeviceController-Plugins-PluginPermissions-DeleteDatabase'></a>
### DeleteDatabase `constants`

##### Summary

Delete database entities (NOTE: Should probably remove since Delete == Write essentially but would be nice to separate it)

<a name='F-ChuckDeviceController-Plugins-PluginPermissions-None'></a>
### None `constants`

##### Summary

No extra permissions

<a name='F-ChuckDeviceController-Plugins-PluginPermissions-ReadDatabase'></a>
### ReadDatabase `constants`

##### Summary

Read database entities

<a name='F-ChuckDeviceController-Plugins-PluginPermissions-WriteDatabase'></a>
### WriteDatabase `constants`

##### Summary

Write database entities

<a name='T-ChuckDeviceController-Plugins-PluginPermissionsAttribute'></a>
## PluginPermissionsAttribute `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Defines which permissions the plugin is going to request
in order to operate correctly.

<a name='M-ChuckDeviceController-Plugins-PluginPermissionsAttribute-#ctor-ChuckDeviceController-Plugins-PluginPermissions-'></a>
### #ctor(permissions) `constructor`

##### Summary

Instantiates a new plugin permissions attribute.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| permissions | [ChuckDeviceController.Plugins.PluginPermissions](#T-ChuckDeviceController-Plugins-PluginPermissions 'ChuckDeviceController.Plugins.PluginPermissions') | Plugin permissions to request upon load. |

<a name='P-ChuckDeviceController-Plugins-PluginPermissionsAttribute-Permissions'></a>
### Permissions `property`

##### Summary

Gets the requested permissions of the plugin.

<a name='T-ChuckDeviceController-Plugins-PluginState'></a>
## PluginState `type`

##### Namespace

ChuckDeviceController.Plugins

##### Summary

Determines the state of a plugin

<a name='F-ChuckDeviceController-Plugins-PluginState-Disabled'></a>
### Disabled `constants`

##### Summary

Plugin has been disabled and is not curretly running
or enabled

<a name='F-ChuckDeviceController-Plugins-PluginState-Error'></a>
### Error `constants`

##### Summary

Plugin has encountered an error and unable to recover

<a name='F-ChuckDeviceController-Plugins-PluginState-Removed'></a>
### Removed `constants`

##### Summary

Plugin has been removed from the host application
and is no longer available

<a name='F-ChuckDeviceController-Plugins-PluginState-Running'></a>
### Running `constants`

##### Summary

Plugin is currently running and active

<a name='F-ChuckDeviceController-Plugins-PluginState-Stopped'></a>
### Stopped `constants`

##### Summary

Plugin has been stopped and is not currently running

<a name='F-ChuckDeviceController-Plugins-PluginState-Unset'></a>
### Unset `constants`

##### Summary

Plugin state has not be set yet
