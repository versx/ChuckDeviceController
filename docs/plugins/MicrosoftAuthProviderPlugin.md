# MicrosoftAuthProviderPlugin  

### Configuration Setup  
[https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins)  

### Callback  
`http(s)://127.0.0.1:8881/signin-microsoft`  

### Example `appsettings.json`  
```json
{
  "Microsoft": {
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
