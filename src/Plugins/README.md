# Plugins  

## Description

Plugins allow for expandability of the application and to introduce more features without having to modify the host application (typically).

## Getting Started

### <u>Plugin Templates for Visual Studio</u>  
Example templates for plugin projects using .NET Core, ASP.NET Core Mvc, and ASP.NET Core Razor are located in the `src/Templates` folder.  

If you plan on creating or modifying plugins, use the following instructions to register the plugin template projects with Visual Studio.  

- Place each .zip template file `.zip` located in the `src/Templates` folder in the Visual Studio project templates directory. By default, this directory is:  
`%USERPROFILE%\Documents\Visual Studio <Version>\Templates\ProjectTemplates\Visual C#\`.

<hr>

## Additional Information


Each plugin's project file i.e. `TestPlugin.csproj` needs the following properties set ([details](https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)):  
```diff
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
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
  * Copy any static files and folders in `wwwroot` to the `src/ChuckDeviceConfigurator/bin/debug/plugins/[PluginName]/wwwroot` folder.
  * Copy any other files the plugin requires (configs, images, etc) other than any `ChuckDeviceController*.dll` libraries to the `src/ChuckDeviceConfigurator/bin/debug/plugins/[PluginName]/` folder.  
- Alternatively you can set an `OnPostBuild` build event in Visual Studio for the plugin to execute the `script/copy.sh` or `script/copy.bat` script to copy the necessary files.  

- **Notes:** Plugin assemblies (`.dll` files) can contain multiple plugin implementations. Although, restricting plugins to one per assembly is recommended for easier maintenance and decoupling, unless each depend on each other.  

- **Important:** When copying the compiled plugin, if you referenced any ChuckDeviceController libraries, do not include them in the plugin folder. This is so the plugin loads the libraries the host application is using rather than the ones local within its domain, which will cause plenty of headaches as versx has already gone through. :)  

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

  <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="6.0.8" />
```

<hr>

## Plugin Configuration  
You will also need to decorate the plugin's main class (the one that inherits from the `IPlugin` interface contract) with an attribute indicating where the static files are located.  
```cs
[StaticFilesLocation(views: StaticFilesLocation.Resources, webRoot: StaticFilesLocation.External)]
public class TestPlugin : IPlugin
{
    ...
}
```

<hr>