# Getting Started

## [Requirements](./requirements.md)  
Ensure all requirements are installed and met before proceeding below.  

<hr>

## Preparation  
1. Clone repository:  
```git clone https://github.com/versx/ChuckDeviceController```  
1. Change directory:  
```cd ChuckDeviceController/src```  
1. Build projects, libraries, plugin templates, and included plugins:  
```dotnet build```  


## ChuckDeviceConfigurator  
1. Change directory:  
```cd ChuckDeviceConfigurator/bin/debug```  
1. Fill out [`appsettings.json`](./applications/configurator.md#configuration) config  
1. Start ChuckDeviceConfigurator:  
```dotnet ChuckDeviceConfigurator.dll```  
1. Visit `http://127.0.0.1:8881` to begin configuring your devices. Change accordingly based on your config options.  
1. Default username is `root` and password is `123Pa$$word.`  


## ChuckDeviceController  
1. Change directory:  
```cd src/ChuckDeviceController/bin/debug```  
1. Fill out [`appsettings.json`](./applications/controller.md#configuration) config  
1. Start ChuckDeviceController:  
```dotnet ChuckDeviceController.dll```  


## ChuckDeviceCommunicator  
1. Change directory:  
```cd src/ChuckDeviceCommunicator/bin/debug```  
1. Fill out [`appsettings.json`](./applications/communicator.md#configuration) config  
1. Start ChuckDeviceCommunicator:  
```dotnet ChuckDeviceCommunicator.dll```  


## ChuckDeviceProxy  
1. Change directory:  
```cd src/ChuckDeviceProxy/bin/debug```  
1. Fill out [`appsettings.json`](./applications/proxy.md#configuration) config  
1. Start ChuckDeviceProxy:  
```dotnet ChuckDeviceProxy.dll```  
