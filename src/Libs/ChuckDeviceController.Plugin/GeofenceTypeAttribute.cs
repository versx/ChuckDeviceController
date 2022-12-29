namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Data.Common;

/// <summary>
/// Sets the expected geofence type for custom
/// <seealso cref="IJobControllerServiceHost"/> instances.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GeofenceTypeAttribute : Attribute
{
    /// <summary>
    /// Gets the specified geofence type expected.
    /// </summary>
    public GeofenceType Type { get; }

    /// <summary>
    /// Instantiates a new instance of the <see cref="GeofenceTypeAttribute"/>
    /// attribute class.
    /// </summary>
    /// <param name="type">Geofence type to define.</param>
    public GeofenceTypeAttribute(GeofenceType type)
    {
        Type = type;
    }
}