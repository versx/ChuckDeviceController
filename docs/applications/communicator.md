# ChuckDeviceCommunicator  

## Configuration  
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