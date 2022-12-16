# Welcome to ChuckDeviceController
ChuckDeviceController is a .NET based frontend and backend written in C# 11.0 using ASP.NET Core, EntityFramework Core, and Dapper.NET to control real devices and parse received protobuff proto data from iOS and Android mobile devices running Pokemon Go.

**ChuckDeviceConfigurator:**  
Controls devices that request jobs as well as includes a dashboard interface to configure job controllers and other required entity types.  
  - Dashboard management UI  
  - Device controller  
  - Plugin system  

**ChuckDeviceController:**  
Parses raw proto data received and inserts/upserts data entities into a MySQL type database.  

**ChuckDeviceCommunicator:**  
Relays new and changed data entities to outgoing endpoints that are received from the ChuckDeviceController via gRPC.  

**ChuckProxy:**  
Splits and proxies requests from Atlas devices to separate endpoints.

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

## Previews
![Dashboard](./images/dashboard.png)

<hr>

## Dedication  
❤️ In loving memory of [Chuckleslove](https://github.com/Chuckleslove), rest in peace brother
