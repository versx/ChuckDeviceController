# ChuckDeviceProxy  

## Description  
Splits and proxies requests from Atlas devices to separate endpoints in order to add support for ChuckDeviceController until Atlas is updated.  

<hr>

## Configuration  
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
  "RawEndpoint": "http://127.0.0.1:8882/raw",
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