# RobotsPlugin  

### Example `appsettings.json`  
```json
{
  "WebCrawler": {
    // Determines whether or not to use the honey pot trap service.
    "UseHoneyPotService": false,
    // Defines the honey pot route to trap any web crawler bots that discover it.
    // Web crawler details (UserAgent and IP Address) will be log to `honeypot.txt` in the plugins root folder.
    "HoneyPotRoute": "/identity/reveal",
    // Determines whether or not to process static files. (i.e. .js, .css, images, etc)
    "ProcessStaticFiles": false,
    // Defines the static file extensions to ignore or include, depending on `ProcessStaticFiles` value.
    "StaticFileExtensions": [
      ".less",
      ".ico",
      ".css",
      ".js",
      ".svg",
      ".jpg",
      ".jpeg",
      ".gif",
      ".png",
      ".eot",
      ".map;"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
