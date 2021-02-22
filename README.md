[![GitHub Contributors](https://img.shields.io/github/contributors/versx/ChuckDeviceController.svg)](https://github.com/versx/ChuckDeviceController/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  
# Chuck Device Controller  

## Supported Databases  
- MySQL 5.7
- MySQL 8.0
- MariaDB 10.3
- MariaDB 10.4
- MariaDB 10.5

## Installation
1. Install [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)  
1. Clone repository `git clone https://github.com/versx/ChuckDeviceController`  
1. Copy config `cp config.example.json` `config.json`  
1. Fill out `config.json`  
1. Build project from root folder `~/.dotnet/dotnet build`  
1. Run from `bin/[debug|release]` folder `~/.dotnet/dotnet ChuckDeviceController.dll`  
1. Visit Dashboard at `http://LAN_MACHINE_IP:5001`

View all available API routes:  
`http://LAN_MACHINE_IP:port/swagger`  

View profiler results:  
`http://LAN_MACHINE_IP:port/profiler/results`  
`http://LAN_MACHINE_IP:port/profiler/results-list`  

## Configuration  
```js
{
    // Change to machine IP address
    "interface": "LAN_MACHINE_IP",
    // Listening port to receive data and control devices
    "port": 5001,
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
    "webhooks": ["http://webhook-address:port"]
}
```
