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