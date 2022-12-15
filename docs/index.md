# Welcome to ChuckDeviceController


## Description
ChuckDeviceController is a .NET based frontend and backend written in C# 10.0 using ASP.NET Core and EntityFramework Core to control real devices and parse received protobuff data from iOS devices running Pokemon Go.

**ChuckDeviceConfigurator:**  
Controls devices that request jobs as well as includes a dashboard interface to configure job controllers and other required entity types.  
  - Dashboard management UI
  - Device controller
  - Plugin system

**ChuckDeviceController:**  
Parses proto data and inserts/upserts data entities into a MySQL type database.  

**ChuckDeviceCommunicator:**  
Relays new and changed data entities to outgoing endpoints that are received from the ChuckDeviceController via gRPC.  

**ChuckProxy:**
Splits and proxies requests from Atlas devices to separate endpoints.

## Features  
- Plugin system  
    * Create new job controller instances  
    * Fetch database entries  
    * Add WebAPI routes and page views to existing dashboard  
    * Much more planned...  
- Job controller instance types  
    * Bootstrap  
    * Dynamic Route
    * Circle Pokemon  
    * Circle Raid  
    * Leveling  
    * Pokemon IV  
    * Quests  
    * Smart Raid
    * Spawnpoint TTH Finder  
    * More planned  
- User and access management system  
    * 2FA capability  
    * 3rd party authentication for Discord, GitHub, and Google accounts as well as local accounts  
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

## Requirements
- .NET 7 SDK  
- MySQL or MariaDB  

### Supported Databases  
- MySQL 5.7
- MySQL 8.0
- MariaDB 10.3
- MariaDB 10.4
- MariaDB 10.5

<hr>


## Frameworks and Libraries
* .NET 7.0
* ASP.NET Core
* EntityFramework Core
* Dapper


## Dedication  
❤️ In loving memory of [Chuckleslove](https://github.com/Chuckleslove), rest in peace brother
