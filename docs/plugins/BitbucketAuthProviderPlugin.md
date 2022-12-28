# BitbucketAuthProviderPlugin  

**Configuration Setup:**  
https://bitbucket.org/{your-workplace}/workspace/settings/oauth-consumers  

**Callback:**  
`http(s)://127.0.0.1:8881/signin-bitbucket`  

**Example `appsettings.json:`**  
```json
{
  "BitBucket": {
    "Enabled": true,
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
