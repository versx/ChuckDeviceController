# ChuckDeviceController  


**ClearFortsHostedService:**  
Soft deletes Pokestop and Gym forts that have not been seen within S2 cells. This will not permanently delete the entities from the database but instead set the `deleted` database columns to `true` as well as the `enabled` columns to `false`.  

