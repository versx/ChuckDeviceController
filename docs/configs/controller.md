# ChuckDeviceController Configuration  

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