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
You may reference any of the existing `ChuckDeviceController.*.dll` project libaries in your plugins. When building and deploying plugins you've developed, do not copy any `ChuckDeviceController.*.dll` libraries to the `src/ChuckDeviceConfigurator/bin/debug/plugins/%PluginFolderName%/` plugin folder. Any 3rd party libraries are fine to copy.

<hr>

### Plugins Included  
- **BitbucketAuthProviderPlugin:**  
Adds `Bitbucket.org` user authentication support    
- **DeviceAuthPlugin:**  
Adds device token and IP based device authentication support
- **Example.DotNetCorePlugin:**  
Very basic 'Clock' plugin example  
- **FindyJumpyPlugin:**  
Adds new Pokemon spawnpoint job controllers  
- **GitLabAuthProviderPlugin:**  
Adds `GitLab.com` user authentication support  
- **HealthChecksPlugin:**  
Adds health checks endpoint and UI  
- **MemoryBenchmarkPlugin:**  
Displays basic memory usage information and chart  
- **MicrosoftAuthProviderPlugin:**  
Adds `Microsoft.com` account authentication support
- **MiniProfilerPlugin:**  
Adds basic profiling options and data.  
- **PogoEventsPlugin:**  
Provides current and upcoming Pokemon Go events.  
- **RazorTestPlugin:**  
Very basic Razor Mvc pages plugin example  
- **RedditAuthProviderPlugin:**  
Adds `Reddit.com` user authentication support  
- **RequestBenchmarkPlugin:**  
Displays web request benchmark times for routes used  
- **RobotsPlugin:**  
Adds web crawler robots management based on specified UserAgent strings and routes which creates a dynamic `robots.txt` file  
- **TestPlugin:**  
In-depth example plugin demonstrating all, if not most, possible functionality of the plugin system  
- **TodoPlugin:**  
Basic TODO list plugin that adds support for keeping track of things to do  
- **VisualStudioAuthProviderPlugin:**  
Adds `VisualStudioOnline.com` user authentication support  