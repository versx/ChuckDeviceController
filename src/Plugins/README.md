# Plugins  

- Each plugin's project file i.e. `TestPlugin.csproj` needs the following properties set ([details](https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)):  
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
  * Copy any Razor/Mvc Views to the `src/ChuckDeviceConfigurator/bin/debug/plugins/Views[controller]/` folder. (temporary for now)  
  * Copy any static files (i.e. `wwwroot` folder) to the `  
  * 

**Important:** When copying the compiled plugin, if you referenced any ChuckDeviceController libraries, do not include them in the plugin folder. This is so the plugin loads the libraries the host application is using rather than the ones local within its domain, which will cause plenty of headaches as versx has already gone through. :)  

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
To configure the plugin to embed the static files (i.e. `wwwroot` folder) in an internal resources file, add the following to the plugin's `.csproj` project file.  
```xml
  <PropertyGroup>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="wwwroot\**\*" />
  </ItemGroup>

  <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="6.0.8" />
```

**Plugin Configuration**  
You will also need to decorate the plugin's main class (the one that inherits from the `IPlugin` interface contract) with an attribute determining where the static files are located.  
```cs
[StaticFilesLocation(StaticFilesLocation.External)]
public class TestPlugin : IPlugin
{
    ...
}
```