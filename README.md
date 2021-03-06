[![Build](https://github.com/versx/ChuckDeviceController/workflows/.NET%20Core/badge.svg)](https://github.com/versx/ChuckDeviceController/actions)
[![GitHub Release](https://img.shields.io/github/release/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/releases/)
[![GitHub Contributors](https://img.shields.io/github/contributors/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  
# Chuck Device Controller  
ChuckDeviceController is a C# based backend using Redis, ASP.NET Core, and EntityFramework Core to control and parse data from iOS devices running Pokemon Go.

## Requirements
- .NET 5 SDK  
- MySQL or MariaDB  
- Redis  

## Supported Databases  
- MySQL 5.7
- MySQL 8.0
- MariaDB 10.3
- MariaDB 10.4
- MariaDB 10.5

## How It Works
There are 4 parts to it:
- ChuckDeviceController which will control devices and includes the device management dashboard UI.
- ChuckProtoParser parses any raw incoming proto data and relays to Redis.  
- DataConsumer will insert or upsert data from the redis queue containing parsed protos to consume with MySQL.
- WebhookProcessor controls and filters webhooks and where to send newly updated events (Pokemon, Raids, etc).

## Installation
### Requirements
1. Install [Redis](https://redis.io/topics/quickstart)  
```
From Source:
wget http://download.redis.io/redis-stable.tar.gz
tar xvzf redis-stable.tar.gz
cd redis-stable
make
src/redis-server

From Ubuntu PPA:
sudo add-apt-repository ppa:redislabs/redis
sudo apt-get update
sudo apt-get install redis
```
1. Install [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)  
```
Ubuntu:
wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y apt-transport-https
sudo apt-get update
sudo apt-get install -y dotnet-sdk-5.0

Windows:
https://download.visualstudio.microsoft.com/download/pr/a105fe06-20a0-4233-8ff1-b85523b40f1d/5f26654016c41ab2dc6d8bc850a9bf4c/dotnet-sdk-5.0.200-win-x64.exe

macOS:
https://download.visualstudio.microsoft.com/download/pr/a06c387d-2811-4fba-8b5f-86cb9f0bdeba/f41d1c63c5b6bcee9293484e845bc518/dotnet-sdk-5.0.200-osx-x64.pkg
```
### ChuckDeviceController
1. Clone repository `git clone https://github.com/versx/ChuckDeviceController`  
1. Copy config `cp config.example.json` `bin/config.json`  
1. Fill out `config.json`  
1. Add `dotnet` to your environment path: `export PATH=~/.dotnet/:$PATH`  
1. Build project from root folder `dotnet build -c Release`
1. Run ChuckDeviceController from `bin/` folder `dotnet ChuckDeviceController.dll`  
1. Run ChuckProtoParser from `bin/` folder `dotnet ChuckProtoParser.dll`  
3. Run WebhookProcessor from `bin/` folder `dotnet WebhookProcessor.dll`  
4. Run DataConsumer from `bin/` folder `dotnet DataConsumer.dll` or use [Chuck](https://github.com/WatWowMap/Chuck) as the backend data consumer.  
5. Visit Dashboard at `http://LAN_MACHINE_IP:5000`  

View all available API routes:  
`http://LAN_MACHINE_IP:port/swagger`  

View profiler results:  
`http://LAN_MACHINE_IP:port/profiler/results`  
`http://LAN_MACHINE_IP:port/profiler/results-list`  

## Configuration  
```json
{
    // Change to machine IP address
    "controllerInterface": "LAN_MACHINE_IP",
    // Listening port to control devices
    "controllerPort": 5000,
    // Change to machine IP address
    "parserInterface": "LAN_MACHINE_IP",
    // Listening port to receive raw proto data
    "parserPort": 5001,
    // Database information
    "db": {
        // Database host/IP address
        "host": "127.0.0.1",
        // Database connection port
        "port": 3306,
        // Database account username
        "username": "cdcuser",
        // Database account password
        "password": "cdcPass!",
        // Database name
        "database": "cdcdb"
    },
    // Redis information
    "redis": {
        // Redis server IP/hostname
        "host": "127.0.0.1",
        // Redis server listening port
        "port": 6379,
        // Redis password/secret
        "password": "",
        // Redis database number to use
        "databaseNum": -1,
        // Redis queue name to use
        "queueName": "cdc"
    }
}
```

## TODO:
- Finish MAD support
- Environment variables
- Webhooks don't currently implement the geofences property
- Localization


## Previews:  
![image](https://user-images.githubusercontent.com/1327440/112744187-a3047280-8f52-11eb-8de9-ebc8eae2d833.png)
