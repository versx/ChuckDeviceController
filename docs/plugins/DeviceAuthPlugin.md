# DeviceAuthPlugin  

**Example `appsettings.json:`**  
```json
{
  "IpAuth": {
    "Enabled": false,
    "IpAddresses": [
      "10.0.0.1/24",
      "10.0.0.1-10.3.0.254",
      "10.0.0.2"
    ]
  },
  "TokenAuth": {
    "Enabled": false,
    "Tokens": []
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