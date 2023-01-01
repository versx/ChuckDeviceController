# Plugins  

## Description

Plugins allow for expandability of the application as well as to introduce more features without modifying the host application (typically).  

## Getting Started  

**Creating a Plugin via provided `Chuck Plugin Templates`**:  
  - Open Visual Studio 2022.  
  - Choose 'Create a new project'.  
  - If you registered the plugin templates, you can search for `Chuck` to show the available CDC plugin templates.  
  - Depending on the type of plugin you want to create, select one of the following:  
    * `ChuckPlugin .NET Core Plugin Template`  
    * `ChuckPluginMvc ASP.NET Core Mvc Plugin Template`  
    * `ChuckPluginMvcApi ASP.NET Core Mvc API Plugin Template`  
    * `ChuckPluginRazor ASP.NET Core Razor Plugin Template`  
  - Configure your new project, set name and location properties.  
  - Click the `Create` button.  

  **Creating a Plugin from scratch**:
  - Open Visual Studio 2022.  
  - Choose 'Create a new project'.  
  - Depending on the type of plugin you want to create, select one of the following:  
    * Class Library (.NET Core/Standard)  
    * ASP.NET Core Web App (Model-View-Controller)  
    * ASP.NET Core Web App (Razor Pages)  
    * ASP.NET Core Web API  (No user interface, just API's)
    * ASP.NET Core Empty (Creates an empty ASP.NET Core project, only advanced/familiar users should choose this option)  
  - Configure your new project, set name and location properties.  
  - Choose Framework backend (.NET 6.0) and other properties.  
  - Click the `Create` button.  
  - Add a reference to the `ChuckDeviceController.Plugin.dll` library. (eventually make nuget package)  
  - Inherit from the `IPlugin` interface contract in your new Plugin class.
  - Implement all default/required interface contact methods and properties.
  - Set name, description, author, and version metadata plugin properties.  
  - We need to change the compiled assembly's 'Output Type' in the menu item `Project->[ProjectName] Properties` from 'Console Application' to 'Class Library'.  
  - All services and classes that are not already registered with the host application, that you would like to use with dependency injection needs to either be decorated with the 'PluginServiceAttribute' attribute for the class. Otherwise you can also register the service via the `ConfigureServices` callback event method. i.e. `services.AddSingleton<IInterfaceContract, InterfaceContractImplementation>()`.  
  - Ensure `Views/_ViewImports.cshtml` exists with at least the following, which allows us to use ASP.NET's built in HTML tag helpers:  
  ```cs
    @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
  ```

<hr>

## Additional Information

Each plugin's project file i.e. `TestPlugin.csproj` needs the following properties set ([reference](https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)):  
```diff
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
+    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>

+  <ItemGroup>
+    <ProjectReference Include="..\..\Libs\ChuckDeviceController.Plugins\ChuckDeviceController.Plugins.csproj">
+      <Private>false</Private>
+      <ExcludeAssets>runtime</ExcludeAssets>
+    </ProjectReference>
+  </ItemGroup>
</Project>
```
- Once the plugin is compiled:  
  * Copy the compiled plugin library (`\*.dll`) to the `src/ChuckDeviceConfigurator/bin/debug/plugins/[PluginName]/` folder.  
  * Copy any Mvc Views to the `src/ChuckDeviceConfigurator/bin/debug/plugins/[PluginName]/Views/[controller]/` folder.  
  * Copy any Razor Pages to the `src/ChuckDeviceConfigurator/bin/debug/plugins/[PluginName]/Pages/[controller]/` folder.  
  * Copy any static files and folders in the `wwwroot` folder to the `src/ChuckDeviceConfigurator/bin/debug/plugins/[PluginName]/wwwroot` folder.
  * Copy any other files the plugin requires or depends on (i.e. config files, dataset files, etc) other than any `ChuckDeviceController*.dll` libraries to the `src/ChuckDeviceConfigurator/bin/debug/plugins/[PluginName]` folder.  
- **Alternatively:** You can set an `OnPostBuild` build event in Visual Studio for the plugin to execute the `scripts/copy.sh` or `scripts/copy.bat` script which will copy the necessary files automatically each successful build.  

- **Notes:** Plugin assemblies (`.dll` files) can contain multiple plugin implementations. Although, limiting plugins to one per assembly is recommended for easier maintenance and decoupling. Otherwise, if the two or more plugins heavily depend/reference each other, combining them into one assembly file would be the better alternative solution.  

- **Important:** When copying the compiled plugin, if you referenced any ChuckDeviceController libraries (including `ChuckDeviceController.Plugin.dll`), do not include them in the `./bin/debug/plugins/[PluginName]` folder. This will instruct the plugin to load the referenced libraries that the host application is using. Rather than the ones local within the plugin's AppDomain, which will cause plenty of headaches as versx has already gone through. :)  

<hr>

## Static Files Configuration
**External / Local**  
To configure the plugin to keep the static files (i.e. `wwwroot` folder) external/local, add the following to the plugin's `.csproj` project file.  
```xml
  <ItemGroup>
    <Content Update="wwwroot\**\*">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
```
**Embedded Resources**  
To configure the plugin to embed the static files (i.e. `wwwroot` folder) in an embedded resources file, add the following to the plugin's `.csproj` project file.  
```xml
  <PropertyGroup>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>

  <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="7.0.1" />
```

<hr>

## Plugin Configuration  
You will also need to decorate the plugin's main class (the one that inherits from the `IPlugin` interface contract) with the `StaticFilesLocationAttribute` attribute indicating where the static files are located.  

It will also need to include the `PluginApiKeyAttribute` attribute specified regardless of what permissions your plugin needs.  
```cs
[StaticFilesLocation(views: StaticFilesLocation.Resources, webRoot: StaticFilesLocation.External)]
[PluginApiKey("CDC-328TVvD7o85TNbNhjLE0JysVMbOxjXKT")]
public class TestPlugin : IPlugin
{
    ...
}
```

<hr>

## Plugin Templates for Visual Studio  
Example templates for plugin projects using .NET Core, ASP.NET Core Mvc, and ASP.NET Core Razor are located in the `src/Templates` folder.  

If you plan on creating plugins, use the following instructions to register the plugin template projects with Visual Studio.  

- Place each `.zip` template file located in the `src/Templates` folder to the Visual Studio project templates directory. By default, this directory is:  
`%USERPROFILE%\Documents\Visual Studio <Version>\Templates\ProjectTemplates\Visual C#\`.