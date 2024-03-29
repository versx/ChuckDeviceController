[![Build](https://github.com/versx/ChuckDeviceController/workflows/.NET/badge.svg)](https://github.com/versx/ChuckDeviceController/actions)
[![Documentation Status](https://readthedocs.org/projects/cdc/badge/?version=latest)](https://cdc.rtfd.io)
[![Docker](https://github.com/versx/ChuckDeviceController/actions/workflows/publish-docker-image.yml/badge.svg)](https://github.com/versx/ChuckDeviceController/actions/workflows/publish-docker-image.yml)
[![GitHub Release](https://img.shields.io/github/release/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/releases/)
[![GitHub Contributors](https://img.shields.io/github/contributors/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  

<p align="center">
  <img src="https://raw.githubusercontent.com/versx/ChuckDeviceController/master/src/ChuckDeviceConfigurator/wwwroot/favicons/chuck.gif" />
</p>
<br>

# Welcome to ChuckDeviceController  
ChuckDeviceController is a .NET based frontend and backend written in C# 11.0 using ASP.NET Core, EntityFramework Core, and Dapper.NET to control real devices and parse received protobuff proto data from iOS and Android mobile devices running Pokemon Go.

<br>

**ChuckDeviceConfigurator**  
Controls devices that request jobs as well as includes a dashboard interface to configure job controllers and other required entity types.  

- Dashboard management UI  
- Device controller  
- Plugin system  

**ChuckDeviceController**  
Parses raw proto data received and inserts/upserts data entities into a MySQL compatible database.  

**ChuckDeviceCommunicator**  
Relays new and changed data entities to outgoing webhook endpoints that are received from the ChuckDeviceController via gRPC.  

**ChuckDeviceProxy**  
Splits and proxies requests from Atlas devices to separate endpoints in order to add support for ChuckDeviceController until Atlas is updated.  

<hr>

## Features  
- Plugin system  
    * Create new job controller instances  
    * Fetch database entries  
    * Add WebAPI routes and page views to existing dashboard  
    * Assign devices to job controller instances  
    * Create instances  
    * Create geofences  
    * Load/save configuration files  
    * Route generator and optimizer  
    * Event bus service for communication between plugins and host application  
    * Add custom settings to the dashboard UI  
    * Much more planned...  
- Job controller instance types  
    * Bootstrap  
    * Dynamic Routes
    * Circle Plots (Pokemon and Raids)  
    * Leveling  
    * Pokemon IV  
    * Quests  
    * Smart Raid
    * Spawnpoint TTH Finder  
    * More planned  
- User and access management system  
    * 2FA capability  
    * 3rd party authentication for Discord, GitHub, and Google accounts as well as local accounts  
- [MAD](https://github.com/Map-A-Droid/MAD) proto data parsing support
- Separate device controller, proto parser & data upsert service as well as a webhook relay service to load balance across multiple machines if needed or desired  
- Reusable Geofence and Circle point lists  
- Reusable IV lists for Pokemon IV job controller instances  
- Quality of life utilities  
    * Clear Quests (by instance, geofence, or all)  
    * Upgraded/downgraded fort converter  
    * Stale Pokestop clearing  
    * Instance reloader  
    * Truncate expired Pokemon and Incident (Invasions) data  
    * Assignments/Assignment groups re-quester  
- and more...  

<hr>

## Documentation  

### Overview  
[https://cdc.rtfd.io](https://cdc.rtfd.io)  
or  
[https://versx.github.io/ChuckDeviceController](https://versx.github.io/ChuckDeviceController)  

### Getting Started  
[https://cdc.rtfd.io/en/latest/getting-started](https://cdc.rtfd.io/en/latest/getting-started)  

### Plugins  
- [Create a Plugin](https://cdc.rtfd.io/en/latest/plugin-system/create-a-plugin)  
- [API Reference](https://cdc.rtfd.io/en/latest/plugin-system/api)  
- [Templates](https://cdc.rtfd.io/en/latest/plugin-system/project-templates)  

<hr>

## Requirements
- [Git](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)  
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)  
- [MySQL](https://dev.mysql.com/downloads/mysql/) or [MariaDB](https://mariadb.org/download/?t=mariadb&p=mariadb)  
    Supported Databases  
    - MySQL 5.7 and MySQL 8.0
    - MariaDB 10.3-10.10

<hr>

## Frameworks and Libraries
* .NET 7
* ASP.NET Core
* EntityFramework Core
* Dapper
* gRPC
* POGOProtos.Rpc

<hr>

## Screenshots
![Dashboard](./img/dashboard.png)

<hr>

## Dedication  
❤️ In loving memory of [Chuckleslove](https://github.com/Chuckleslove), rest in peace brother
