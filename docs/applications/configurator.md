# ChuckDeviceConfigurator  

## User Identity 

### QR Code Generation  
### Account Confirmation and Password Recovery  

<hr>

## Hosted Services  

### AccountStatusHostedService  
Checks all accounts with any of the following status and marks the interested columns as `NULL` in the database if the punishment time has lapsed:  

- `Warning`  
- `Suspended`  
- `Cooldown`  

<hr>

## Configuration  
```json
{
  // Hosts allowed to access.
  "AllowedHosts": "*",
  // Default 3rd party authentication options.
  "Authentication": {
    // Discord user account authentication.
    "Discord": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    },
    // GitHub user account authentication.
    "GitHub": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    },
    // Google user account authentication.
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