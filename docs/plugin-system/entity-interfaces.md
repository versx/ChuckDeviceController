# Plugin Entity Interface Contracts (IDatabaseHost)  

All available database entity model interface contracts.  

## Controller Entity Types:  
- [IAccount](#iaccount)
- [IAssignment](#iassignment)
- [IAssignmentGroup](#iassignmentgroup)
- [IDevice](#idevice)
- [IDeviceGroup](#idevicegroup)
- [IGeofence](#igeofence)
- [IInstance](#iinstance)
- [IIvList](#iivlist)
- [IWebhook](#iwebhook)

## Map Entity Types:  
- [ICell](#icell)
- [IGym](#igym)
- [IGymDefender](#igymdefender)
- [IGymTrainer](#igymtrainer)
- [IIncident](#iincident)
- [IPokemon](#ipokemon)
- [IPokestop](#ipokestop)
- [ISpawnpoint](#ispawnpoint)
- [IWeather](#iweather)

### IAccount  
Properties:  
```cs
string Username;
string Password;
ushort Level;
ulong? FirstWarningTimestamp;
ulong? FailedTimestamp;
string? Failed;
ulong? LastEncounterTime;
double? LastEncounterLatitude;
double? LastEncounterLongitude;
uint Spins;
ushort Tutorial;
ulong? CreationTimestamp;
bool? HasWarn;
ulong? WarnExpireTimestamp;
bool? WarnMessageAcknowledged;
bool? SuspendedMessageAcknowledged;
bool? WasSuspended;
bool? IsBanned;
ulong? LastUsedTimestamp;
string? GroupName;
```

### IAssignment  
Properties:  
```cs
uint Id
string InstanceName
string? SourceInstanceName
string? DeviceUuid
uint Time
DateTime? Date
string? DeviceGroupName
bool Enabled
```

### IAssignmentGroup  
Properties:  
```cs
string Name
List<uint> AssignmentIds
```

### IDevice  
Properties:  
```cs
string Uuid
string? InstanceName
string? AccountUsername
string? LastHost
double? LastLatitude
double? LastLongitude
ulong? LastSeen // Last job request requested
bool IsPendingAccountSwitch // used internally
```

### IDeviceGroup  
Properties:  
```cs
string Name
List<string> DeviceUuids
```

### IGeofence  
Properties:  
```cs
string Name
GeofenceType Type
GeofenceData? Data
```

### IInstance  
Properties:  
```cs
string Name
InstanceType Type
ushort MinimumLevel
ushort MaximumLevel
List<string> Geofences
InstanceData? Data
```

### IIvList  
Properties:  
```cs
string Name
List<string> PokemonIds
```

### IWebhook  
Properties:  
```cs
string Name
List<WebhookType> Types
double Delay
string Url
bool Enabled
List<string> Geofences
WebhookData? Data
```

<hr>

### ICell  
Properties:  
```cs
ulong Id
ushort Level
double Latitude
double Longitude
ulong Updated
```

### IGym  
Properties:  
```cs
string Id
string? Name
string? Url
double Latitude
double Longitude
ulong LastModifiedTimestamp
ulong? RaidEndTimestamp
ulong? RaidSpawnTimestamp
ulong? RaidBattleTimestamp
ulong Updated
uint? RaidPokemonId
uint GuardingPokemonId
ushort AvailableSlots
// TODO: Team Team
ushort? RaidLevel
bool IsEnabled
bool IsExRaidEligible
bool InBattle
uint? RaidPokemonMove1
uint? RaidPokemonMove2
uint? RaidPokemonForm
uint? RaidPokemonCostume
uint? RaidPokemonCP
uint? RaidPokemonEvolution
ushort? RaidPokemonGender
bool? RaidIsExclusive
ulong CellId
bool IsDeleted
int TotalCP
ulong FirstSeenTimestamp
uint? SponsorId
bool? IsArScanEligible
uint? PowerUpPoints
ushort? PowerUpLevel
ulong? PowerUpEndTimestamp
```

### IGymDefender  
Properties:  
```cs
ulong Id
string Nickname
ushort PokemonId
ushort DisplayPokemonId
ushort Form
ushort Costume
// TODO: Gender Gender
uint CpWhenDeployed
uint CpNow
uint Cp
uint BattlesWon
uint BattlesLost
double BerryValue
uint TimesFed
ulong DeploymentDuration
string? TrainerName
string? FortId
ushort AttackIV
ushort DefenseIV
ushort StaminaIV
ushort Move1
ushort Move2
ushort Move3
uint BattlesAttacked
uint BattlesDefended
double BuddyKmWalked
uint BuddyCandyAwarded
uint CoinsReturned
bool FromFort
bool HatchedFromEgg
bool IsBad
bool IsEgg
bool IsLucky
bool IsShiny
uint PvpCombatWon
uint PvpCombatTotal
uint NpcCombatWon
uint NpcCombatTotal
double HeightM
double WeightKg
ulong Updated
```

### IGymTrainer  
Properties:  
```cs
string Name
ushort Level
// TODO: Team TeamId
uint BattlesWon
double KmWalked
ulong PokemonCaught
ulong Experience
ulong CombatRank
ulong CombatRating
bool HasSharedExPass
ushort GymBadgeType
ulong Updated
```

### IIncident  
Properties:  
```cs
string Id
string? PokestopId
ulong Start
ulong Expiration
uint DisplayType
uint Style
ushort Character
ulong Updated
```

### IPokemon  
Properties:  
```cs
string Id
uint PokemonId
double Latitude
double Longitude
ulong? SpawnId
ulong ExpireTimestamp
ushort? AttackIV
ushort? DefenseIV
ushort? StaminaIV
double? IV
ushort? Move1
ushort? Move2
ushort? Gender
ushort? Form
ushort? Costume
ushort? CP
ushort? Level
double? Weight
double? Size
ushort? Weather
bool? IsShiny
string? Username
string? PokestopId
ulong? FirstSeenTimestamp
ulong Updated
ulong Changed
ulong CellId
bool IsExpireTimestampVerified
double? Capture1
double? Capture2
double? Capture3
bool IsDitto
uint? DisplayPokemonId
Dictionary<string, dynamic>? PvpRankings
double BaseHeight
double BaseWeight
bool IsEvent
SeenType SeenType
```

### IPokestop  
Properties:  
```cs
string Id
double Latitude
double Longitude
string? Name
string? Url
ushort LureId
ulong? LureExpireTimestamp
ulong LastModifiedTimestamp
ulong Updated
bool IsEnabled
ulong CellId
bool IsDeleted
ulong FirstSeenTimestamp
uint? SponsorId
bool IsArScanEligible
uint? PowerUpPoints
ushort? PowerUpLevel
ulong? PowerUpEndTimestamp

#region Quests
uint? QuestType
string? QuestTemplate
string? QuestTitle
ushort? QuestTarget
ulong? QuestTimestamp

#region Virtual Columns (Automatically Generated)
ushort? QuestRewardType
ushort? QuestItemId
ushort? QuestRewardAmount
uint? QuestPokemonId
#endregion

List<Dictionary<string, dynamic>>? QuestConditions
List<Dictionary<string, dynamic>>? QuestRewards
uint? AlternativeQuestType
string? AlternativeQuestTemplate
string? AlternativeQuestTitle
ushort? AlternativeQuestTarget
ulong? AlternativeQuestTimestamp

#region Virtual Columns (Automatically Generated)
ushort? AlternativeQuestRewardType
ushort? AlternativeQuestItemId
ushort? AlternativeQuestRewardAmount
uint? AlternativeQuestPokemonId
#endregion

List<Dictionary<string, dynamic>>? AlternativeQuestConditions
List<Dictionary<string, dynamic>>? AlternativeQuestRewards
#endregion
```

### ISpawnpoint  
Properties:  
```cs
ulong Id
double Latitude
double Longitude
uint? DespawnSecond
ulong Updated
ulong? LastSeen
```

### IWeather  
Properties:  
```cs
long Id
ushort Level
double Latitude
double Longitude
// TODO: WeatherCondition GameplayCondition
ushort WindDirection
ushort CloudLevel
ushort RainLevel
ushort WindLevel
ushort SnowLevel
ushort FogLevel
ushort SpecialEffectLevel
ushort? Severity
bool? WarnWeather
ulong Updated
```
