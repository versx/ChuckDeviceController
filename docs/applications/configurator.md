# ChuckDeviceConfigurator  


**AccountStatusHostedService:**  
Checks all accounts with any of the following status and marks the interested columns as `NULL` in the database if the punishment time has lapsed:  
- `Warning`  
- `Suspended`  
- `Cooldown`  