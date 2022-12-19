[![Build](https://github.com/versx/ChuckDeviceController/workflows/.NET/badge.svg)](https://github.com/versx/ChuckDeviceController/actions)
[![Documentation Status](https://readthedocs.org/projects/cdc/badge/?version=latest)](https://cdc.rtfd.io)
[![Docker](https://github.com/versx/ChuckDeviceController/actions/workflows/publish-docker-image.yml/badge.svg)](https://github.com/versx/ChuckDeviceController/actions/workflows/publish-docker-image.yml)
[![GitHub Release](https://img.shields.io/github/release/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/releases/)
[![GitHub Contributors](https://img.shields.io/github/contributors/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  

![](https://raw.githubusercontent.com/versx/ChuckDeviceController/net6/src/ChuckDeviceConfigurator/wwwroot/favicons/chuck.gif)

# ChuckDeviceController  
ChuckDeviceController is a .NET based frontend and backend written in C# 11.0 using ASP.NET Core, EntityFramework Core, and Dapper.NET to control real devices and parse received protobuff proto data from iOS and Android mobile devices running Pokemon Go.

**ChuckDeviceConfigurator:**  
Controls devices that request jobs as well as includes a dashboard interface to configure job controllers and other required entity types.  

- Dashboard management UI  
- Device controller  
- Plugin system  

**ChuckDeviceController:**  
Parses raw proto data received and inserts/upserts data entities into a MySQL type database.  

**ChuckDeviceCommunicator:**  
Relays new and changed data entities to outgoing endpoints that are received from the ChuckDeviceController via gRPC.  

**ChuckProxy:**  
Splits and proxies requests from Atlas devices to separate endpoints.

<hr>


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

## Documentation  

### Overview  
https://cdc.rtfd.io

### Getting Started  
https://cdc.rtfd.io/en/latest/getting-started

### Plugin API Reference  
https://cdc.rtfd.io/en/latest/plugin-system/api  

### Plugin Development  
https://cdc.readthedocs.io/en/latest/plugin-system

<hr>

## Requirements
- [Git](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)  
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)  
- [MySQL](https://dev.mysql.com/downloads/mysql/) or [MariaDB](https://mariadb.org/download/?t=mariadb&p=mariadb)  
    Supported Databases  
    - MySQL 5.7 and MySQL 8.0  
    - MariaDB 10.3-10.10  

### Install Git
**Debian-based:** `sudo apt install git-all`  
**Windows:** [https://git-scm.com/download/win](https://git-scm.com/download/win)  
**macOS:**  
Homebrew:
```
brew install git
```  
[Other macOS Installations](https://git-scm.com/download/mac)   

### Install .NET 7 SDK
**Ubuntu:** (Replace `{22,20,18}` with your respective major OS version)  
```
wget https://packages.microsoft.com/config/ubuntu/{22,20,18}.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update- 
sudo apt-get install -y apt-transport-https
sudo apt-get update
sudo apt-get install -y dotnet-sdk-7.0
```
[Other Linux Distributions](https://learn.microsoft.com/en-us/dotnet/core/install/linux)  

**Windows:**  
```
x86: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-windows-x86-installer

x64: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-windows-x64-installer

ARM64: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-windows-arm64-installer
```

**macOS:**
```
Intel:
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-macos-x64-installer

Apple Silicon:
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.101-macos-arm64-installer
```

### MySQL Database
[Install](https://dev.mysql.com/downloads/installer)  

or

### MariaDB Database
[Install](https://mariadb.org/download/?t=mariadb)


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

### ChuckProxy (optional)  
1. Change directory: `cd src/ChuckProxy/bin/debug`  
1. Fill out [`appsettings.json`](./configs/proxy.md) config  
1. Start ChuckProxy `dotnet ChuckProxy.dll`  


View all available API routes:  
`http://LAN_MACHINE_IP:port/swagger`  

<hr>

## Configuration  

### ChuckDeviceController (Protobuf Proto Parser & Data Inserter)  
```json
{
  // Hosts allowed to access.
  "AllowedHosts": "*",
  // Determines whether automatic migrations are enabled when set to `true`.
  // Otherwise manual migrations are required. i.e. `dotnet ef database update`.
  "AutomaticMigrations": true,
  "Cache": {
    // Memory cache settings.
    "CompactionPercentage": 0.25,
    // Expiration time limit for entities in minutes.
    "EntityExpiryLimitM": 30,
    // Entity names to cache (leave as is if you don't know what you're doing).
    "EntityTypeNames": [
      "Account",
      "Device",
      "Cell",
      "Gym",
      "Incident",
      "Pokemon",
      "Pokestop",
      "Spawnpoint",
      "Weather"
    ],
    // Interval at which cached entities are checked to see if expired or not.
    "ExpirationScanFrequencyM": 5,
    // Size limit of memory cache.
    "SizeLimit": 10240
  },
  // Database connection strings.
  "ConnectionStrings": {
    "DefaultConnection": "Uid=cdcuser;Password=cdcpass123;Host=127.0.0.1;Port=3306;Database=cdcdb;old guids=true;Allow User Variables=true;"
  },
  // Determines whether to convert MAD proto data to compatible payload format.
  "ConvertMadData": false,
  // Database settings.
  "Database": {
    // Maximum timeout in seconds before a command is aborted.
    "CommandTimeoutS": 30,
    // Maximum timeout in seconds before a connection is determined as a leak.
    // When a connection is assumed a leak, it is aborted.
    "ConnectionLeakTimeoutS": 120,
    // Maximum timeout in seconds before a connection is aborted.
    "ConnectionTimeoutS": 30,
    // Maximum amount of retries upon failed connection.
    "MaximumRetryCount": 30,
    // Connection pool size.
    "PoolSize": 1024,
    // Timeout in seconds between attemping failed connections.
    "RetryIntervalS": 10
  },
  // gRPC listener endpoints.
  "Grpc": {
    // gRPC listener endpoint of ChuckDeviceConfigurator. Used to relay scanned Pokemon statistics, trainer account information, as well as gym information.
    "Configurator": "http://localhost:5002",
    // gRPC listener endpoint of ChuckDeviceCommunicator. Used to relay processed data entities ready to send to configured webhook endpoints.
    "Communicator": "http://localhost:5003"
  },
  /* Reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-7.0 */
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Grpc.Core": "None",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware": "None",
      "Microsoft.EntityFrameworkCore": "Information",
      "ChuckDeviceConfigurator": "Trace",
      "ChuckDeviceController": "Trace",
      "ChuckDeviceCommunicator": "Trace"
    },
    "ColorConsole": {
      "LogLevel": {
        "Default": "Trace",
        "Grpc.Core": "None",
        "Grpc.Net.Client": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware": "None",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.Extensions": "Warning",
        "System.Net.Http.HttpClient": "Warning",
        "ChuckDeviceConfigurator": "Trace",
        "ChuckDeviceController": "Trace",
        "ChuckDeviceCommunicator": "Trace"
      },
      "LogLevelColorMap": {
        "Trace": "Cyan",
        "Debug": "DarkGray",
        "Information": "White",
        "Warning": "Yellow",
        "Error": "Red",
        "Critical": "DarkRed"
      },
      "UseTimestamp": true,
      "UseUnix": false,
      "TimestampFormat": "{0:HH}:{0:mm}:{0:ss}"
    },
    "Console": {
      "LogLevel": {
        "Default": "Debug",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware": "None",
        "Microsoft.EntityFrameworkCore": "Information",
        "Microsoft.Extensions": "Information",
        "ChuckDeviceConfigurator": "Trace",
        "ChuckDeviceController": "Trace",
        "ChuckDeviceCommunicator": "Trace"
      }
    },
    "File": {
      "Path": "bin/debug/logs/{0:yyyy}-{0:MM}-{0:dd}.log",
      "Append": true,
      "MinLevel": "Debug",
      "FileSizeLimitBytes": 0, // use to activate rolling file behaviour
      "MaxRollingFiles": 0 // use to specify max number of log files
    }
  },
  "GymOptions": {
    "ExRaidBossId": 150,
    "ExRaidBossForm": 0
  },
  "PokemonOptions": {
    "EnablePvp": false,
    "EnableMapPokemon": false,
    "EnableWeatherIvClearing": false,
    "SaveSpawnpointLastSeen": true
  },
  // Default Pokestop settings.
  "PokestopOptions": {
    // Lure time in seconds (Default: 30 seconds)
    "LureTimeS": 1800
  },
  // Data processing settings.
  "ProcessingOptions": {
    // Proto processing service settings.
    "Protos": {
      // Interval in seconds between batch processing of received protos.
      "IntervalS": 3,
      "LogLevel": "Summary",
      // Show processing benchmark times. (i.e. `Processed 64 protos in 1.234s`)
      "ShowProcessingTimes": true,
      // Show processing benchmark counts. (i.e. `Processed 1/20 weather cells`)
      "ShowProcessingCount": true,
      // Decimal precision of benchmark times when `ShowProcessingTimes` is enabled.
      "DecimalPrecision": 5,
      // Determines whether AR quests are allowed or not.
      "AllowArQuests": true,
      // Determines whether to process map/lure Pokemon encounters.
      "ProcessMapPokemon": true,
      // Proto processing queue settings.
      "Queue": {
        // Maximum amount of protos to batch when processing.
        "MaximumBatchSize": 100,
        // Maximum size of queue before warning message is shown.
        "MaximumSizeWarning": 1024,
        // Maximum queue capacity.
        "MaximumQueueCapacity": 10240
      }
    },
    // Data entities processing service settings.
    "Data": {
      // Soft delete any forts that have upgraded, downgraded, or removed.
      "ClearOldForts": true,
      // Interval in seconds between batch processing data entities.
      "IntervalS": 3,
      "LogLevel": "Summary",
      // Show processing benchmark times. (i.e. `Processed 1422 entities in 1.234s`)
      "ShowProcessingTimes": true,
      // Show processing benchmark counts. (i.e. `Processed 1/20 weather cells`) Disabled by default and should only be used for debugging.
      "ShowProcessingCount": false,
      // Decimal precision of benchmark times when `ShowProcessingTimes` is enabled.
      "DecimalPrecision": 4,
      // Concurrency level for processing data entities. Basically how many parsers active at once.
      "ParsingConcurrencyLevel": 15,
      // Determines whether or not to process player account protos.
      "ProcessPlayerData": true,
      // Determines whether or not to process S2 cell protos.
      "ProcessCells": true,
      // Determines whether or not to process S2 client weather cell protos.
      "ProcessWeather": true,
      // Determines whether or not to process fort protos.
      "ProcessForts": true,
      // Determines whether or not to process fort details protos.
      "ProcessFortDetails": true,
      // Determines whether or not to process Gym info protos.
      "ProcessGymInfo": true,
      // Determines whether or not to process Gym defenders from `GymInfo`.
      "ProcessGymDefenders": true,
      // Determines whether or not to process Gym trainers from `GymInfo` protos.
      "ProcessGymTrainers": true,
      // Determines whether or not to process Pokestop incidents.
      "ProcessIncidents": true,
      // Determines whether or not to process wild Pokemon protos.
      "ProcessWildPokemon": true,
      // Determines whether or not to process nearby Pokemon protos.
      "ProcessNearbyPokemon": true,
      // Determines whether or not to process map/lure Pokemon protos.
      "ProcessMapPokemon": true,
      // Determines whether or not to process Pokestop quest protos.
      "ProcessQuests": true,
      // Determines whether or not to process Pokemon encounter protos.
      "ProcessEncounters": true,
      // Determines whether or not to process Pokemon disk encounter protos.
      "ProcessDiskEncounters": true,
      // Data entity processing queue settings.
      "Queue": {
        // Maximum amount of entities to batch when processing.
        "MaximumBatchSize": 100,
        // Maximum size of queue before warning message is shown.
        "MaximumSizeWarning": 1024,
        // Maximum queue capacity.
        "MaximumQueueCapacity": 10240
      }
    },
    // Data entity consumer service settings.
    "Consumer": {
      // Interval in seconds between batch insert/upsert of processed data entities.
      "IntervalS": 5,
      "LogLevel": "Summary",
      // Show processing benchmark times. (i.e. `Consumed 1422 entities in 1.234s`)
      "ShowProcessingTimes": true,
      // Not currently used.
      "ShowProcessingCount": true,
      // Decimal precision of benchmark times when `ShowProcessingTimes` is enabled.
      "DecimalPrecision": 3,
      // Not currently used. Transactions are always used.
      "UseTransactions": true,
      // Data entity consumer processing queue settings.
      "Queue": {
        // Maximum amount of entities to batch upsert.
        "MaximumBatchSize": 5000,
        // Maximum size of queue before warning message is shown.
        "MaximumSizeWarning": 10240,
        // Maximum queue capacity.
        "MaximumQueueCapacity": 1048576
      },
      // Concurrency level for consuming processed data entities. Basically how many parsers active at once * CPU core count.
      "QueueConcurrencyLevelMultiplier": 10
    }
  },
  // Statistics database triggers settings.
  "StatisticTriggers": {
    // Determines whether to enable Pokemon database triggers. (Not currently used)
    "Pokemon": false
  },
  // Listening endpoint and port to receive proto data from devices. Multiple endpoints are supported, use semicolons (`;`) as delimiter between endpoints.
  "Urls": "http://*:8888",
  // Webhook settings.
  "Webhooks": {
    // Determines whether to relay webhook payloads to ChuckDeviceCommunicator.
    "Enabled": false
  }
}

```

### ChuckDeviceConfigurator (Dashboard/Management UI & Device Controller)  
```json
{
  // Hosts allowed to access.
  "AllowedHosts": "*",
  // 3rd party authentication options.
  "Authentication": {
    // Discord user authentication.
    "Discord": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    },
    // GitHub user authentication.
    "GitHub": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    },
    // Google user authentication.
    "Google": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  // Determines whether automatic migrations are enabled when set to `true`.
  // Otherwise manual migrations are required. i.e. `dotnet ef database update`.
  "AutomaticMigrations": true,
  // Memory cache settings.
  "Cache": {
    // Compactions percentage for cached entities.
    "CompactionPercentage": 0.25,
    // Expiration time limit for entities in minutes.
    "EntityExpiryLimitM": 15,
    // Entity names to cache (leave as is if you don't know what you're doing).
    "EntityTypeNames": [
      "Account",
      "Device"
    ],
    // Interval at which cached entities are checked to see if expired or not.
    "ExpirationScanFrequencyM": 5,
    // Size limit of memory cache.
    "SizeLimit": 10240
  },
  // Database connection strings.
  "ConnectionStrings": {
    "DefaultConnection": "Uid=cdcuser;Password=cdcpass123;Host=127.0.0.1;Port=3306;Database=cdcdb;old guids=true;Allow User Variables=true;"
  },
  // Database settings.
  "Database": {
    // Maximum timeout in seconds before a command is aborted.
    "CommandTimeoutS": 30,
    // Maximum timeout in seconds before a connection is determined as a leak.
    // When a connection is assumed a leak, it is aborted.
    "ConnectionLeakTimeoutS": 120,
    // Maximum timeout in seconds before a connection is aborted.
    "ConnectionTimeoutS": 30,
    // Maximum amount of retries upon failed connection.
    "MaximumRetryCount": 30,
    // Connection pool size.
    "PoolSize": 1024,
    // Timeout in seconds between attemping failed connections.
    "RetryIntervalS": 10
  },
  // Json web tokens authentication settings.
  "Jwt": {
    // Determines whether JWT authentication is enabled or not.
    "Enabled": false,
    // Host of JWT issuer.
    "Issuer": "http://127.0.0.1:8881/",
    // Host of JWT audience.
    "Audience": "http://127.0.0.1:8881/",
    // JWT signing key.
    "Key": "JwtExampleSecretKey_MakeSureYouChangeThisToA_SecureRandomizedValue",
    // JWT token expiration time in minutes. (Default: 30 days)
    "TokenValidityM": 43200
  },
  // Listening endpoints settings
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http1AndHttp2"
    },
    "Endpoints": {
      "Http": {
        // Configurator listening endpoint and port.
        "Url": "http://*:8881"
      },
      "Grpc": {
        // gRPC listening endpoint and port to receive data from ChuckDeviceController.
        "Url": "http://*:5002",
        "Protocols": "Http2"
      }
    }
  },
  // API keys.
  "Keys": {
    // SendGrid email service API key.
    "SendGridKey": ""
  },
  // Locale to load and use.
  "Locale": "en",
  /* Reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-7.0 */
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "ColorConsole": {
      "LogLevel": {
        "Default": "Debug",
        "Grpc.Core": "None",
        "Grpc.Net.Client": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware": "None",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.Extensions": "Warning",
        "System.Net.Http.HttpClient": "Warning",
        "ChuckDeviceConfigurator": "Trace",
        "ChuckDeviceController": "Trace",
        "ChuckDeviceCommunicator": "Trace"
      },
      "LogLevelColorMap": {
        "Trace": "Cyan",
        "Debug": "DarkGray",
        "Information": "White",
        "Warning": "Yellow",
        "Error": "Red",
        "Critical": "DarkRed"
      },
      "UseTimestamp": true,
      "UseUnix": false,
      "TimestampFormat": "{0:HH}:{0:mm}:{0:ss}"
    },
    "Console": {
      "LogLevel": {
        "Default": "Debug",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware": "None"
      }
    },
    "File": {
      "Path": "bin/debug/logs/{0:yyyy}-{0:MM}-{0:dd}.log",
      "Append": true,
      "MinLevel": "Debug",
      "FileSizeLimitBytes": 0, // use to activate rolling file behaviour
      "MaxRollingFiles": 0 // use to specify max number of log files
    }
  },
  // Default map settings for any leaflet map instances.
  "Map": {
    // Starting latitude.
    "StartLatitude": 0,
    // Starting longitude.
    "StartLongitude": 0,
    // Starting zoom.
    "StartZoom": 13,
    // Minimum zoom level.
    "MinimumZoom": 4,
    // Maximum zoom level.
    "MaximumZoom": 18,
    // Leaflet map tileserver url.
    "TileserverUrl": "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
  },
  // Dashboard UI theme.
  "Theme": "dark",
  // User accounts settings
  "UserAccounts": {
    // User account lock out settings.
    "Lockout": {
      "AllowedForNewUsers": true,
      "MaxFailedAccessAttempts": 5,
      "DefaultLockoutTimeSpan": 15
    },
    // User account password settings.
    "Password": {
      "RequireDigit": true,
      "RequiredLength": 8,
      "RequiredUniqueChars": 1,
      "RequireLowercase": true,
      "RequireUppercase": true,
      "RequireNonAlphanumeric": true
    },
    // User account sign-in settings.
    "SignIn": {
      "RequireConfirmedAccount": true,
      "RequireConfirmedEmail": true,
      "RequireConfirmedPhoneNumber": false
    },
    // User account settings.
    "User": {
      "AllowedUserNameCharacters": "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+",
      "RequireUniqueEmail": true
    }
  }
}
```

### ChuckDeviceCommunicator (Webhook Relay Service)  
```json
{
  // Hosts allowed to access.
  "AllowedHosts": "*",
  // gRPC listener endpoints.
  "Grpc": {
    // gRPC listener endpoint of ChuckDeviceConfigurator. Used to retrieve configured webhook endpoints.
    "Configurator": "http://localhost:5002"
  },
  // Listening endpoints settings
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http1AndHttp2"
    },
    "Endpoints": {
      "Grpc": {
        // gRPC listening endpoint and port to receive webhook payloads from ChuckDeviceController.
        "Url": "http://*:5003",
        "Protocols": "Http2"
      }
    }
  },
  /* Reference: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-7.0 */
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware": "None"
    },
    "ColorConsole": {
      "LogLevel": {
        "Default": "Debug",
        "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware": "None"
      },
      "LogLevelColorMap": {
        "Trace": "Cyan",
        "Debug": "DarkGray",
        "Information": "White",
        "Warning": "Yellow",
        "Error": "Red",
        "Critical": "DarkRed"
      },
      "UseTimestamp": true,
      "UseUnix": false,
      "TimestampFormat": "{0:HH}:{0:mm}:{0:ss}"
    },
    "Console": {
      "LogLevel": {
        "Default": "Debug",
        "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware": "None"
      }
    },
    "File": {
      "Path": "bin/debug/logs/{0:yyyy}-{0:MM}-{0:dd}.log",
      "Append": true,
      "MinLevel": "Debug",
      "FileSizeLimitBytes": 0, // use to activate rolling file behaviour
      "MaxRollingFiles": 0 // use to specify max number of log files
    }
  },
  // Webhook relay settings.
  "Relay": {
    // Webhook endpoints retrieval interval in seconds.
    "EndpointsIntervalS": 60,
    // Retry delay upon failed webhook relay.
    "FailedRetryDelayS": 5,
    // Maximum amount of retries when sending failed webhook payloads.
    "MaximumRetryCount": 3,
    // Interval in seconds between processing received webhook payloads.
    "ProcessingIntervalS": 5,
    // Timeout in seconds when sending webhook payload before aborting.
    "RequestTimeoutS": 15
  }
}

```

### ChuckProxy (Atlas Workaround)  
```json
{
  // Listening endpoint and port to receive proto data from Atlas devices which
  // will be relayed to the below configured endpoints.
  // Multiple endpoints are supported, use semicolons (`;`) as delimiter
  // between endpoints.
  "Urls": "http://*:5151",
  // ChuckDeviceConfigurator device controller endpoint to proxy.
  "ControllerEndpoint": "http://127.0.0.1:8881/controler",
  // ChuckDeviceController proto data endpoint to proxy.
  "RawEndpoints": [
    "http://127.0.0.1:8882/raw"
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  // Hosts allowed to access.
  "AllowedHosts": "*"
}

```

<hr>

## Plugins Included  
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

<hr>

## Previews:  
![Dashboard](docs/images/dashboard.png)  

<hr>

## TODO:  
- [ ] Finish localization  
- [ ] Finish TTH finder job controller  
- [ ] Finish implementing permissions provided by API keys  
- [ ] Finish documentation  
- [ ] Finish plugin service event callbacks  
- [ ] Finish event service bus  
- [ ] Finish plugin build scripts  
- [ ] Add more helper methods to ChuckDeviceController.Plugin.Helpers library  

<hr>

## Dedication  
❤️ In loving memory of [Chuckleslove](https://github.com/Chuckleslove), rest in peace brother
