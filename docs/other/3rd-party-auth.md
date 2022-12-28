# 3rd Party OAuth Authentication Providers  

Modify the below configuration file `auth_providers.json` to customize the displayed login buttons for each 3rd party authentication provider.  

### Microsoft Documentation  
[https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social)  

### Fontawesome Icons  
[https://fontawesome.com/search?m=free](https://fontawesome.com/search?m=free)  

### Configuration Location  
`src/ChuckDeviceConfigurator/bin/Debug/wwwroot/data/auth_providers.json`  

### Example  
```json
{
  "Discord": {
    "Icon": "fa-brands fa-discord fa-align-left social-icon",
    "Class": "",
    "Style": "background: #5865F2; color: #fff;"
  },
  "GitHub": {
    "Icon": "fa-brands fa-github fa-align-left social-icon",
    "Class": "",
    "Style": "background: #000000; color: #fff;"
  },
  "Google": {
    "Icon": "fa-brands fa-google fa-align-left social-icon",
    "Class": "",
    "Style": "background: #d24228; color: #fff;"
  },
  "Reddit": {
    "Icon": "fa-brands fa-reddit fa-align-left social-icon",
    "Class": "",
    "Style": "background: #FF4500; color: #fff;"
  },
  "GitLab": {
    "Icon": "fa-brands fa-gitlab fa-align-left social-icon",
    "Class": "",
    "Style": "background: #E24329; color: #fff;"
  },
  "Bitbucket": {
    "Icon": "fa-brands fa-bitbucket fa-align-left social-icon",
    "Class": "",
    "Style": "background: #2684FF; color: #fff;"
  },
  "Visual Studio Online": {
    "Icon": "fa-brands fa-microsoft fa-align-left social-icon",
    "Class": "",
    "Style": "background: #3376D0; color: #fff;"
  },
  "Microsoft": {
    "Icon": "fa-brands fa-microsoft fa-align-left social-icon",
    "Class": "",
    "Style": "background: #3376D0; color: #fff;"
  }
}
```