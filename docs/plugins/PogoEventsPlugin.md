# PogoEventsPlugin  

### Example `appsettings.json`  
```json
{
  "Discord": {
    // Whether or not to enable Discord posting of new events
    "Enabled": false,
    // Discord bot token
    "Token": "",
    "LogLevel": "Information",
    // Dictionary of Discord Guilds
    "Guilds": {
      // Discord Guild ID
      "0000000": {
        // User, Role, and Channel mention strings to include in new event posts
        "Mention": "",
        // Events channel ID to post new events
        "EventsChannelId": "000000",
        // Events category channel ID to create active event channels
        "EventsCategoryChannelId": "000000",
        // Whether to delete previous events that have expired
        "DeletePreviousEvents": false,
        // Active event channel naming format
        // Default order:
        // - {0}: Month
        // - {1}: Day
        // - {2}: Event Name
        "ChannelNameFormat": "{0}-{1} {2}",
        // Discord User IDs to send new event posts
        "UserIds": []
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```