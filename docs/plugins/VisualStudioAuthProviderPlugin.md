# VisualStudioAuthProviderPlugin  

### Configuration Setup  
[https://app.vssps.visualstudio.com](https://app.vssps.visualstudio.com)  

### Callback  
`http(s)://127.0.0.1:8881/signin-visualstudio`  

### Example `appsettings.json`  
```json
{
  "VisualStudio": {
    "Enabled": false,
    "ClientId": "",
    "ClientSecret": ""
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
