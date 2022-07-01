[![Build](https://github.com/versx/ChuckDeviceController/workflows/.NET/badge.svg)](https://github.com/versx/ChuckDeviceController/actions)
[![GitHub Release](https://img.shields.io/github/release/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/releases/)
[![GitHub Contributors](https://img.shields.io/github/contributors/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  

![](https://raw.githubusercontent.com/versx/ChuckDeviceController/net6/src/ChuckDeviceConfigurator/wwwroot/favicons/chuck.gif)

# Chuck Device Controller  
ChuckDeviceController is a C# based backend written in .NET 6.0 using ASP.NET Core and EntityFramework Core to control and parse protobuff proto data from iOS devices running Pokemon Go.


## Requirements
- .NET 6 SDK  
- MySQL or MariaDB  


## Supported Databases  
- MySQL 5.7
- MySQL 8.0
- MariaDB 10.3
- MariaDB 10.4
- MariaDB 10.5

<hr>

## Installation
### Requirements
1. Install [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)  

**Ubuntu:** (Replace `{22,20,18}` with your respective major OS version)  
```
wget https://packages.microsoft.com/config/ubuntu/{22,20,18}.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update- 
sudo apt-get install -y apt-transport-https
sudo apt-get update
sudo apt-get install -y dotnet-sdk-6.0
```

**Windows:**  
```
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.301-windows-x64-installer
```

**macOS:**
```
Intel: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.301-macos-x64-installer
Apple Silicon: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.301-macos-arm64-installer
```

<hr>

## Getting Started  
1. Clone repository `git clone https://github.com/versx/ChuckDeviceController -b net6 cdc && cd cdc`  
1. Add `dotnet` to your environment path: `export PATH=~/.dotnet/:$PATH`  
1. Change directories to `src` folder: `cd src`  
1. Build project solution: `dotnet build`  

### ChuckDeviceController  
1. Copy ChuckDeviceController config `cp ChuckDeviceController/appsettings.json bin/debug/appsettings.json`  
1. Change directories `cd bin/debug` and fill out `appsettings.json` configuration file  
1. Run ChuckDeviceController `dotnet ChuckDeviceController.dll`  

### ChuckDeviceConfigurator  
1. Copy ChuckDeviceConfigurator config `cp ChuckDeviceConfigurator/appsettings.json bin/debug/appsettings.json`  
1. Change directories `cd bin/debug` and fill out `appsettings.json` configuration file  
1. Run ChuckDeviceConfigurator `dotnet ChuckDeviceConfigurator.dll`  
1. Visit Dashboard at `http://LAN_MACHINE_IP:8881`  


View all available API routes:  
`http://LAN_MACHINE_IP:port/swagger`  

<hr>

## Configuration  

### ChuckDeviceController (Protobuf Proto Parser & Data Inserter)  
```json
{
  "ConnectionStrings": {
    // Database connection string
    "DefaultConnection": "Uid=cdcuser;Password=cdcpass123;Host=127.0.0.1;Port=3306;Database=cdcdb;old guids=true;Allow User Variables=true;"
  },
  // Webhook data endpoint
  "Urls": "http://*:8888",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### ChuckDeviceConfigurator (Dashboard/Management UI)  
```json
{
{
  "ConnectionStrings": {
    // Database connection string
    "DefaultConnection": "Uid=cdcuser;Password=cdcpass123;Host=127.0.0.1;Port=3306;Database=cdcdb;old guids=true;Allow User Variables=true;"
  },
  // Available authentication providers
  "Authentication": {
    "Discord": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    },
    "GitHub": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    },
    "Google": {
      "Enabled": false,
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  // Url to access the Dashboard UI
  "Urls": "http://*:8881",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

<hr>

## TODO:  
- Implement Pokemon Pvp rankings
- Finish Dashboard UI  
- Improve database performance  
- Switch from EFCore to Dapper or other alternative  
- Add inter-process communication  
- Fix automated database migrator  
- User authentication system (Use UserIdentity)  
- Responsive pages  
- Add MAD support  
- Webhooks service  
- Localization  

<hr>

## Previews:  
![image](https://user-images.githubusercontent.com/1327440/112744187-a3047280-8f52-11eb-8de9-ebc8eae2d833.png)
