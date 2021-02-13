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
1. Run from `bin/` folder `~/.dotnet/dotnet ChuckDeviceController.dll`  
1. Visit Dashboard at http://localhost:5000 or https://localhost:5001

View all available routes:  
http://localhost:port/swagger