# Plugins  

- Each plugin's project file i.e. `TestPlugin.csproj` needs the following properties set:  
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
  * Copy the compiled plugin library (`\*.dll`) to the `src/ChuckDeviceConfigurator/bin/debug/plugins` folder.  
  * Copy any Razor Views to the `src/Views/[controller]/` folder. (temporary for now)  
  * 

**Important:** When copying the compiled plugin, if you referenced any ChuckDeviceController libraries, do not include them in the plugin folder. This is so the plugin loads the libraries the host application is using rather than the ones local within its domain, which will cause plenty of headaches as versx has already gone through. :)  