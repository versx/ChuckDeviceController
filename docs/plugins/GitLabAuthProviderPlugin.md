# GitLabAuthProviderPlugin  

**Configuration Setup:**  
[https://gitlab.com/-/profile/applications](https://gitlab.com/-/profile/applications)  

**Callback:**  
`http(s)://127.0.0.1:8881/signin-gitlab`  

**Example `appsettings.json:`**  
```json
{
  "GitLab": {
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
