# RedditAuthProviderPlugin  

**Configuration Setup:**  
https://www.reddit.com/prefs/apps   

**Callback:**  
`http(s)://127.0.0.1:8881/signin-reddit`  

**Example `appsettings.json:`**  
```json
{
  "Reddit": {
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
