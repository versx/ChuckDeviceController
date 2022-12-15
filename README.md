[![Build](https://github.com/versx/ChuckDeviceController/workflows/.NET/badge.svg)](https://github.com/versx/ChuckDeviceController/actions)
[![Documentation Status](https://readthedocs.org/projects/cdc/badge/?version=latest)](https://cdc.rtfd.io)
[![GitHub Release](https://img.shields.io/github/release/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/releases/)
[![GitHub Contributors](https://img.shields.io/github/contributors/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  

![](https://raw.githubusercontent.com/versx/ChuckDeviceController/net6/src/ChuckDeviceConfigurator/wwwroot/favicons/chuck.gif)

# Chuck Device Controller  
ChuckDeviceController is a .NET based frontend and backend written in C# 10.0 using ASP.NET Core and EntityFramework Core to control real devices and parse received protobuff data from iOS devices running Pokemon Go.


## Features  
- Plugin system  
    * Create new job controller instances  
    * Fetch database entries  
    * Add WebAPI routes and page views to existing dashboard  
    * Assign devices to job controller instances  
    * Create instances  
    * Create geofences  
    * Load/save configuration files  
    * Route generator and optimizer  
    * Event bus service for communication between plugins and host application  
    * Add custom settings to the dashboard UI  
    * Much more planned...  
- Job controller instance types  
    * Bootstrap  
    * Dynamic Route
    * Circle Pokemon  
    * Circle Raid  
    * Leveling  
    * Pokemon IV  
    * Quests  
    * Smart Raid
    * Spawnpoint TTH Finder  
    * More planned  
- User and access management system  
    * 2FA capability  
    * 3rd party authentication for Discord, GitHub, and Google accounts as well as local accounts  
- [MAD](https://github.com/Map-A-Droid/MAD) proto data parsing support
- Separate device controller, proto parser & data upsert service as well as a webhook relay service to load balance across multiple machines if needed or desired  
- Reusable Geofence and Circle point lists  
- Reusable IV lists for Pokemon IV job controller instances  
- Quality of life utilities  
    * Clear Quests (by instance, geofence, or all)  
    * Upgraded/downgraded fort converter  
    * Stale Pokestop clearing  
    * Instance reloader  
    * Truncate expired Pokemon and Incident (Invasions) data  
    * Assignments/Assignment groups re-quester  
- and more...  

<hr>

## Installation

### Supported Databases  
- MySQL 5.7
- MySQL 8.0
- MariaDB 10.3+

### Requirements
- [Git](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)  
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)  
- [MySQL](https://dev.mysql.com/downloads/mysql/) or [MariaDB](https://mariadb.org/download/?t=mariadb&p=mariadb)  

**Ubuntu:** (Replace `{22,20,18}` with your respective major OS version)  
```
wget https://packages.microsoft.com/config/ubuntu/{22,20,18}.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update- 
sudo apt-get install -y apt-transport-https
sudo apt-get update
sudo apt-get install -y dotnet-sdk-6.0
```

**Windows:**  
```
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.301-windows-x64-installer
```

**macOS:**
```
Intel: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.301-macos-x64-installer
Apple Silicon: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.301-macos-arm64-installer
```

<hr>

## Getting Started  
1. Clone repository `git clone https://github.com/versx/ChuckDeviceController -b net6 cdc && cd cdc`  
1. Add `dotnet` to your environment path: `export PATH=~/.dotnet/:$PATH`  
1. Change directories to `src` folder: `cd src`  
1. Build project solution: `dotnet build`  

### ChuckDeviceConfigurator  
1. Copy ChuckDeviceConfigurator config `cp ChuckDeviceConfigurator/appsettings.json bin/debug/appsettings.json`  
1. Change directories `cd bin/debug` and fill out `appsettings.json` configuration file  
1. Run ChuckDeviceConfigurator `dotnet ChuckDeviceConfigurator.dll`  
1. Visit Dashboard at `http://LAN_MACHINE_IP:8881`  

### ChuckDeviceController  
1. Copy ChuckDeviceController config `cp ChuckDeviceController/appsettings.json bin/debug/appsettings.json`  
1. Change directories `cd bin/debug` and fill out `appsettings.json` configuration file  
1. Run ChuckDeviceController `dotnet ChuckDeviceController.dll`   
1. Point devices `Data Endpoint` to `http://LAN_MACHINE_IP:8888` to start processing and consuming all received proto data  

### ChuckDeviceCommunicator (optional)  
1. Copy ChuckDeviceCommunicator config `cp ChuckDeviceCommunicator/appsettings.json bin/debug/appsettings.json`  
1. Change directories `cd bin/debug` and fill out `appsettings.json` configuration file  
1. Run ChuckDeviceCommunicator `dotnet ChuckDeviceCommunicator.dll`  

View all available API routes:  
`http://LAN_MACHINE_IP:port/swagger`  

<hr>

## Configuration  

### ChuckDeviceController (Protobuf Proto Parser & Data Inserter)  
```json
{
  "ConnectionStrings": {
    // Database connection string
    "DefaultConnection": "Uid=cdcuser;Password=cdcpass123;Host=127.0.0.1;Port=3306;Database=cdcdb;old guids=true;Allow User Variables=true;"
  },
  // Enables automatic migrations if set, otherwise `dotnet ef migrations` tool
  // will need to be run manually to migrate database tables
  "AutomaticMigrations": true,
  // Enables database event triggers for historical Pokemon, Raid, Quest, and Invasion statistics
  "StatisticTriggers": {
    "Pokemon": false
  },
  // Proto data endpoint
  "Urls": "http://*:8888",
  // gRPC service used to communicate with the configurator
  "GrpcConfiguratorServer": "http://localhost:5002",
  // gRPC service used to communicate/relay webhooks to the webhook service
  "GrpcWebhookServer": "http://localhost:5003",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Options": {
    // When enabled, automatically clears (marks 'deleted') upgraded or downgraded
    // Pokestops and Gyms
    "ClearOldForts": true,
    // Determines whether or not to process map (aka Lure) Pokemon spawns
    "ProcessMapPokemon": true
  },
  // Converts incoming proto data from MAD devices to a compatible format
  "ConvertMadData": false,
  "AllowedHosts": "*"
}
```

### ChuckDeviceConfigurator (Dashboard/Management UI & Device Controller)  
```json
{
{
  "ConnectionStrings": {
    // Database connection string
    "DefaultConnection": "Uid=cdcuser;Password=cdcpass123;Host=127.0.0.1;Port=3306;Database=cdcdb;old guids=true;Allow User Variables=true;"
  },
  "Keys": {
    // SendGrid API key, used for email service
    "SendGridKey": ""
  },
  // Enables automatic migrations if set, otherwise `dotnet ef migrations` tool
  // will need to be run manually to migrate database tables
  "AutomaticMigrations": true,
  // Available authentication providers
  "Authentication": {
    "Discord": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    },
    "GitHub": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    },
    "Google": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  "Kestrel": {
    "EndpointDefaults": {
        "Protocols": "Http1AndHttp2"
    },
    "Endpoints": {
      // Endpoint used to access the Dashboard UI
      "Http": {
        "Url": "http://*:8881"
      },
      // Endpoint used for inter-process communication between controller and webhook relay service
      "Grpc": {
        "Url": "http://*:5002",
        "Protocols": "Http2"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Map": {
    "StartLatitude": 0,
    "StartLongitude": 0,
    "StartZoom": 13,
    "MinimumZoom": 4,
    "MaximumZoom": 18,
    "TileserverUrl": "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
  },
  // Possible theme values: dark, light
  "Theme": "dark",
  "AllowedHosts": "*"
}
```

### ChuckDeviceCommunicator (Webhook Relay Service)  
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  // Endpoint of the configurator's gRPC service (used to request updated webhook endpoints)
  "GrpcConfiguratorServer": "http://localhost:5002",
  // Webhook relay settings
  "Relay": {
    // Maximum amount of attempts to try resending a webhook that initially failed before
    // aborting the request entirely
    "MaximumRetryCount": 3,
    // Request timeout limit in seconds when sending the webhook to the endpoint before
    // it aborts if no response
    "RequestTimeout": 30
  },
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http1AndHttp2"
    },
    "Endpoints": {
      "Grpc": {
        // Listening endpoint where webhooks will be sent via gRPC
        "Url": "http://*:5003",
        "Protocols": "Http2"
      }
    }
  }
}
```

<hr>

## Available Plugins  
- BitbucketAuthProviderPlugin: Adds `Bitbucket.org` user authentication support    
- DeviceAuthPlugin: Adds device token and IP based device authentication support
- Example.DotNetCorePlugin: Very basic 'Clock' plugin example  
- GitLabAuthProviderPlugin: Adds `GitLab.com` user authentication support  
- MemoryBenchmarkPlugin: Displays basic memory usage information and chart  
- PogoEventsPlugin: Provides current and upcoming Pokemon Go events.  
- RazorTestPlugin: Very basic Razor pages plugin example  
- RedditAuthProviderPlugin: Adds `Reddit.com` user authentication support  
- RequestBenchmarkPlugin: Displays web request benchmark times for routes used  
- RobotsPlugin: Adds web crawler robots management based on specified UserAgent strings and routes which creates a dynamic `robots.txt` file  
- TestPlugin: In-depth example plugin demonstrating all, if not most, possible functionality of the plugin system  
- TodoPlugin: Basic TODO list plugin that adds support for keeping track of things to do  
- VisualStudioAuthProviderPlugin: Adds `VisualStudioOnline.com` user authentication support  

<hr>

## TODO:  
- Improve database performance  
- Localization  
- Finish TTH finder job controller

<hr>

## Previews:  
TODO  

<hr>

## Dedication  
❤️ In loving memory of [Chuckleslove](https://github.com/Chuckleslove), rest in peace brother