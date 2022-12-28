# Features  

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
