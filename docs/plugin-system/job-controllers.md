# New Job Controller Instances  

When creating a plugin that includes a new job controller instance type inheriting from the `IJobControllerServiceHost` interface contract, the job controller class will need to be decorated by the `GeofenceTypeAttribute` attribute. This will determine what type of geofence object to pass to the job controller when it is instantiated.  

At a minimum, the constructor of the new job controller will need to include `IInstance` and `List<List<ICoordinate>>` or `List<IMultiPolygon>` types (depending what you specified for the `GeofenceTypeAttribute`). If you specified `GeofenceType.Geofence` both `List<List<ICoordinate>>` and `List<IMultiPolygon>` will be available depending on your preference of geofence objects to use.  

<br>  

> NOTE: The order of the parameters passed to the constructor does not matter, this will be figured out by the plugin system.  

<br>


## Example  
```cs
[GeofenceType(GeofenceType.Geofence)]
public class TestJobController
    : IJobController, IJobControllerCoordinates, IScanNextInstanceController
{
    public TestJobController(
        IInstance instance,
        List<List<ICoordinate>> coords,
        List<IMultiPolygon> multiPolygons,
        IDatabaseHost dbHost,
        IGeofenceServiceHost geofenceHost,
        ILoggingHost loggingHost)
    {
        Name = instance.Name;
        MinimumLevel = instance.MinimumLevel;
        MaximumLevel = instance.MaximumLevel;
        GroupName = instance.Data?.AccountGroup ?? null;
        IsEvent = instance.Data?.IsEvent ?? false;
        Coordinates = coords?.FirstOrDefault()!;
        ScanNextCoordinates = new();
        MultiPolygons = multiPolygons;

        _dbHost = dbHost;
        _geofenceHost = geofenceHost;
        _loggingHost = loggingHost;
    }
}
```

## Available Interface Contracts  
To extend the new job controller type more, you can inherit from any of the following interface contracts.  

**IEventInstanceController**  
```cs
/// <summary>
/// Unique event job controller.
/// </summary>
public interface IEventInstanceController
{
    /// <summary>
    /// Gets a unique group name to use with job controller instances
    /// to group related accounts with.
    /// </summary>
    string? GroupName { get; }

    /// <summary>
    /// Gets a value determining whether the instance is for an event or
    /// not. Returns <c>true</c> if it is an event, otherwise <c>false</c>.
    /// </summary>
    bool IsEvent { get; }
}
```

**IJobControllerCoordinates**  
```cs
/// <summary>
/// Job controller uses coordinates instead of geofences.
/// </summary>
public interface IJobControllerCoordinates
{
    /// <summary>
    /// Gets a list of coordinate objects.
    /// </summary>
    IReadOnlyList<ICoordinate> Coordinates { get; }
}
```

**IJobControllerGeofences**  
```cs
/// <summary>
/// Job controller uses geofences instead of coordinates.
/// </summary>
public interface IJobControllerGeofences
{
    /// <summary>
    /// Gets a list of geofence objects.
    /// </summary>
    IReadOnlyList<IMultiPolygon> MultiPolygons { get; }
}
```

**IEventInstanceController**  
```cs
/// <summary>
/// Unique event job controller.
/// </summary>
public interface IEventInstanceController
{
    /// <summary>
    /// Gets a unique group name to use with job controller instances
    /// to group related accounts with.
    /// </summary>
    string? GroupName { get; }

    /// <summary>
    /// Gets a value determining whether the instance is for an event or
    /// not. Returns <c>true</c> if it is an event, otherwise <c>false</c>.
    /// </summary>
    bool IsEvent { get; }
}
```

**ILureInstanceController**  
```cs
/// <summary>
/// Manages lure pokemon encounters.
/// </summary>
public interface ILureInstanceController
{
    /// <summary>
    /// Gets a value determining whether lure encounters are enabled or not.
    /// </summary>
    public bool EnableLureEncounters { get; }
}
```

**IScanNextInstanceController**  
```cs
/// <summary>
/// Allows for 'ScanNext' API scanning.
/// </summary>
public interface IScanNextInstanceController
{
    /// <summary>
    /// Gets a queue of coordinates to pokemon to scan.
    /// </summary>
    Queue<ICoordinate> ScanNextCoordinates { get; }
}
```