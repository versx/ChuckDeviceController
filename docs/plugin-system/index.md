# Plugin System  

[.NET Documentation](https://learn.microsoft.com/en-us/dotnet/?view=aspnetcore-7.0)

## <u>Requirements</u>  
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)  
- [ChuckPlugin Templates (optional but recommended)](./project-templates.md)  

<hr>

## <u>Creating a Plugin</u>  
Microsoft's ASP.NET Core MVC Views and Razor Pages are very similar to Mustache, Handlebars.js, and other templating engines.  
1. Open Visual Studio 2022  
1. Select `Create a new project`  
1. Click the search box showing `Search for templates`
1. Enter the search keyword `chuck`
1. Select the plugin type you want to use
1. Start developing your plugin


**Library References:**  
You may reference any of the existing `ChuckDeviceController.*.dll` project libaries in your plugins. When building and deploying plugins you've developed, do not copy any `ChuckDeviceController.*.dll` libraries to the `bin/debug/plugins/%PluginFolderName%/` plugin folder. Any 3rd party libraries are fine to copy.
