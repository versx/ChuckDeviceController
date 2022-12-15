# Getting Started


TODO: Create separate page for prerequisites
## Prerequisites
- [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)  
- [MySQL](https://dev.mysql.com/downloads/mysql/) or [MariaDB](https://mariadb.org/download/?t=mariadb)  


## ChuckDeviceConfigurator  
1. Clone repository: `git clone https://github.com/versx/ChuckDeviceController`  
1. Change directory: `cd ChuckDeviceController/src`  
1. Build project and libraries: `dotnet build`  
1. Change directory: `cd ChuckDeviceConfigurator/bin/debug`  
1. Fill out [`appsettings.json`](./configs/configurator.md) config  
1. Start ChuckDeviceConfigurator `dotnet ChuckDeviceConfigurator.dll`  
1. Visit `http://127.0.0.1:8881` (default, change accordingly to your config options) to begin configuring your devices.  
1. Default username is `root` and password is `123Pa$$word.`  


## ChuckDeviceController  
1. Change directory: `cd src/ChuckDeviceController/bin/debug`  
1. Fill out [`appsettings.json`](./configs/controller.md) config  
1. Start ChuckDeviceController `dotnet ChuckDeviceController.dll`  


## ChuckDeviceCommunicator  
1. Change directory: `cd src/ChuckDeviceCommunicator/bin/debug`  
1. Fill out [`appsettings.json`](./configs/communicator.md) config  
1. Start ChuckDeviceCommunicator `dotnet ChuckDeviceCommunicator.dll`  


## ChuckProxy  
1. Change directory: `cd src/ChuckProxy/bin/debug`  
1. Fill out [`appsettings.json`](./configs/proxy.md) config  
1. Start ChuckProxy `dotnet ChuckProxy.dll`  